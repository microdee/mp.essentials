using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using SlimDX;
using VVVV.Utils.IO;
using VVVV.Utils.SlimDX;
using VVVV.Utils.VMath;

namespace mp.essentials.Camera
{
    public struct CameraProperties
    {
        public Matrix4x4 InputView;
        public Vector3D Translation;
        public Quaternion Rotation;
        public double PivotDistance;
        public double Fov;
        public double Near;
        public double Far;

        /// <summary>
        /// Create new
        /// </summary>
        /// <param name="inputView"></param>
        /// <param name="translation"></param>
        /// <param name="rotation"></param>
        /// <param name="pivotDistance"></param>
        /// <param name="fov"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        public CameraProperties(
            Matrix4x4 inputView = default,
            Vector3D translation = default,
            Quaternion rotation = default,
            double pivotDistance = 0,
            double fov = 0.15,
            double near = 0.1,
            double far = 100)
        {
            InputView = inputView == default ? VMath.IdentityMatrix : inputView;
            Translation = translation;
            Rotation = rotation == default ? Quaternion.Identity : rotation;
            PivotDistance = pivotDistance;
            Fov = fov;
            Near = near;
            Far = far;
        }

        /// <summary>
        /// Copy
        /// </summary>
        /// <param name="prev"></param>
        public CameraProperties(CameraProperties prev)
        {
            InputView = prev.InputView;
            Translation = prev.Translation;
            Rotation = prev.Rotation;
            PivotDistance = prev.PivotDistance;
            Fov = prev.Fov;
            Near = prev.Near;
            Far = prev.Far;
        }

        /// <summary>
        /// Apply delta
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="delta"></param>
        /// <param name="view"></param>
        /// <param name="rotSpeed"></param>
        /// <param name="frametime"></param>
        public CameraProperties(
            CameraProperties prev,
            CameraDelta delta,
            Matrix4x4 view,
            double rotSpeed = 1,
            double frametime = 1) : this(prev)
        {
            var rotmat = new Matrix4x4(VMath.Inverse(view))
            {
                row4 = new Vector4D(0, 0, 0, 1)
            };
            if (delta.SetTranslation)
                Translation = prev.Translation + rotmat * (delta.Translation * frametime);

            if (delta.SetRotation)
            {
                var inputrotmat = new Matrix4x4(InputView)
                {
                    row4 = new Vector4D(0, 0, 0, 1)
                };
                var inrotq = Quaternion.RotationMatrix(inputrotmat.ToSlimDXMatrix());
                var rottime = frametime * rotSpeed;
                var rotq = Quaternion.RotationYawPitchRoll((float) (delta.PitchYawRoll.y * rottime),
                    (float) (delta.PitchYawRoll.x * rottime), (float) (delta.PitchYawRoll.z * rottime));
                //Rotation = Quaternion.Normalize(inrotq * rotq * Quaternion.Invert(inrotq) * Rotation);
                Rotation = Quaternion.Normalize(prev.Rotation * Quaternion.Invert(inrotq) * rotq * inrotq);
            }

            if (delta.SetPivotDistance)
                PivotDistance = Math.Max(0, prev.PivotDistance + delta.PivotDistance * frametime);

            if (delta.SetFov)
            {
                var nfov = VMath.Map(prev.Fov, 0.01, 0.45, 0, 1, TMapMode.Clamp);
                nfov += delta.Fov * frametime * (nfov + 0.05);
                Fov = VMath.Map(nfov, 0, 1, 0.01, 0.45, TMapMode.Clamp);
            }

            if (delta.SetNear)
                Near = Math.Max(0, prev.Near + delta.Near * frametime);

            if (delta.SetFar)
                Far = Math.Max(0, prev.Far + delta.Far * frametime);
        }

        /// <summary>
        /// Selectively reset to default
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="def"></param>
        /// <param name="delta"></param>
        public CameraProperties(
            CameraProperties prev,
            CameraProperties def,
            CameraDelta delta) : this(prev)
        {
            if (delta.ResetTranslation) Translation = def.Translation;
            if (delta.ResetRotation) Rotation = def.Rotation;
            if (delta.ResetPivotDistance) PivotDistance = def.PivotDistance;
            if (delta.ResetFov) Fov = def.Fov;
            if (delta.ResetNear) Near = def.Near;
            if (delta.ResetFar) Far = def.Far;
        }

        public ulong GetViewChecksum()
        {
            unsafe
            {
                double inp = Translation.x; ulong res = *(ulong*)&inp;
                inp = Translation.y; res ^= *(ulong*)&inp;
                inp = Translation.z; res ^= *(ulong*)&inp;
                inp = Rotation.X; res ^= *(ulong*)&inp;
                inp = Rotation.Y; res ^= *(ulong*)&inp;
                inp = Rotation.Z; res ^= *(ulong*)&inp;
                inp = Rotation.W; res ^= *(ulong*)&inp;
                inp = PivotDistance; res ^= *(ulong*)&inp;

                return InputView.Values.Aggregate(
                    res,
                    (current, value) => current ^ (*(ulong*)&value)
                );
            }
        }

        public ulong GetProjChecksum()
        {
            unsafe
            {
                double inp = Fov; ulong res = *(ulong*)&inp;
                inp = Near; res ^= *(ulong*)&inp;
                inp = Far; res ^= *(ulong*)&inp;
                return res;
            }
        }

        public Matrix4x4 GetView() =>
            VMath.Translate(Translation) *
            Matrix.RotationQuaternion(Rotation).ToMatrix4x4() *
            VMath.Translate(0, 0, PivotDistance) * InputView;

        public Matrix4x4 GetProj() =>
            VMath.PerspectiveLH(Fov, Near, Far, 1.0);

        public override bool Equals(object obj)
        {
            if (obj is CameraProperties other)
            {
                return GetViewChecksum() == other.GetViewChecksum() &&
                       GetProjChecksum() == other.GetProjChecksum();
            }
            return false;
        }

        public override int GetHashCode()
        {
            unsafe
            {
                ulong vchkl = GetViewChecksum();
                ulong vchkl0 = (vchkl & 0xFFFFFFFF00000000) >> 32;
                ulong vchkl1 = vchkl & 0x00000000FFFFFFFF;
                int vchki = (*(int*)&vchkl0) ^ (*(int*)&vchkl1);

                ulong pchkl = GetProjChecksum();
                ulong pchkl0 = (pchkl & 0xFFFFFFFF00000000) >> 32;
                ulong pchkl1 = pchkl & 0x00000000FFFFFFFF;
                int pchki = (*(int*)&pchkl0) ^ (*(int*)&pchkl1);

                return vchki ^ pchki;
            }
        }

        public CameraProperties Lerp(CameraProperties other, double alpha)
        {
            return new CameraProperties(
                InputView,
                VMath.Lerp(Translation, other.Translation, alpha),
                Quaternion.Slerp(Rotation, other.Rotation, (float)alpha),
                VMath.Lerp(PivotDistance, other.PivotDistance, alpha),
                VMath.Lerp(Fov, other.Fov, alpha),
                Near, Far
            );
        }
    }

    public class ModularCam
    {
        public CameraProperties Default { get; set; }

        public CameraProperties Properties { get; set; }

        public Matrix4x4 InputAspect { get; set; } = VMath.IdentityMatrix;
        public double RotationSpeed { get; set; } = 1;

        public ulong ViewChecksum => Properties.GetViewChecksum();
        private ulong _prevViewChecksum;

        public ulong ProjectionChecksum => Properties.GetProjChecksum();
        private ulong _prevProjectionChecksum = 0;

        public Matrix4x4 View
        {
            get
            {
                var chks = ViewChecksum;
                if (chks != _prevViewChecksum)
                {
                    _view = Properties.GetView();
                }
                _prevViewChecksum = chks;
                return _view;
            }
        }
        public Matrix4x4 ViewInverse => VMath.Inverse(View);
        private Matrix4x4 _view;

        public Matrix4x4 Projection
        {
            get
            {
                var chks = ProjectionChecksum;
                if (chks != _prevProjectionChecksum)
                {
                    _proj = Properties.GetProj();
                }
                _prevProjectionChecksum = chks;
                return _proj;
            }
        }
        public Matrix4x4 ProjectionInverse => VMath.Inverse(Projection);
        private Matrix4x4 _proj;

        public Matrix4x4 ProjectionWithAspect => Projection * VMath.Inverse(InputAspect);
        public Matrix4x4 ProjectionWithAspectInverse => VMath.Inverse(Projection);

        public void Move(CameraDelta delta, double frametime = 1)
        {
            Properties = new CameraProperties(
                Properties, delta, View, RotationSpeed, frametime
            );
            Properties = new CameraProperties(Properties, Default, delta);
        }
    }

    public class CameraDelta
    {
        private Vector3D _translation;
        private Vector3D _pitchYawRoll;
        private double _pivotDistance;
        private double _fov;
        private double _near;
        private double _far;

        public ModularCam ConnectedCamera { get; set; }
        public bool SetTranslation { get; private set; }
        public bool SetRotation { get; private set; }
        public bool SetPivotDistance { get; private set; }
        public bool SetFov { get; private set; }
        public bool SetNear { get; private set; }
        public bool SetFar { get; private set; }

        public bool ResetTranslation { get; set; }
        public bool ResetRotation { get; set; }
        public bool ResetPivotDistance { get; set; }
        public bool ResetFov { get; set; }
        public bool ResetNear { get; set; }
        public bool ResetFar { get; set; }

        public Vector3D Translation
        {
            get => _translation;
            set
            {
                SetTranslation = value.Length > 0.000001;
                _translation = value;
            }
        }

        public Vector3D PitchYawRoll
        {
            get => _pitchYawRoll;
            set
            {
                SetRotation = value.Length > 0.000001;
                _pitchYawRoll = value;
            }
        }

        public double PivotDistance
        {
            get => _pivotDistance;
            set
            {
                SetPivotDistance = Math.Abs(value) > 0.000001;
                _pivotDistance = value;
            }
        }

        public double Fov
        {
            get => _fov;
            set
            {
                SetFov = Math.Abs(value) > 0.000001;
                _fov = value;
            }
        }

        public double Near
        {
            get => _near;
            set
            {
                SetNear = Math.Abs(value) > 0.000001;
                _near = value;
            }
        }

        public double Far
        {
            get => _far;
            set
            {
                SetFar = Math.Abs(value) > 0.000001;
                _far = value;
            }
        }

        public bool InteractUpstream { get; set; }
        public bool LockCursor { get; set; }
        public Mouse InputMouse { get; set; }
        public Keyboard InputKeyboard { get; set; }

        public CameraDelta()
        {
            ResetSignals();
        }

        public void ResetSignals()
        {
            SetTranslation = false;
            SetRotation = false;
            SetPivotDistance = false;
            SetFov = false;
            SetFar = false;
            SetNear = false;
            ResetTranslation = false;
            ResetRotation = false;
            ResetPivotDistance = false;
            ResetFov = false;
            ResetFar = false;
            ResetNear = false;
        }
    }
}
