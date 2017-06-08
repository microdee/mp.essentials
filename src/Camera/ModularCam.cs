using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using VVVV.Utils.IO;
using VVVV.Utils.SlimDX;
using VVVV.Utils.VMath;

namespace mp.essentials.Camera
{
    public class ModularCam
    {
        public ModularCam Default { get; set; }

        public Vector3D Translation { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public double PivotDistance { get; set; }
        public double Fov { get; set; } = 0.25;
        public double Near { get; set; } = 0.1;
        public double Far { get; set; } = 1000;

        public Matrix4x4 InputView { get; set; } = VMath.IdentityMatrix;
        public Matrix4x4 InputAspect { get; set; } = VMath.IdentityMatrix;
        public double RotationSpeed { get; set; } = 1;

        public string ViewChecksum
        {
            get
            {
                var tempstream = new MemoryStream();
                tempstream.Write(BitConverter.GetBytes(Translation.x), 0, 8);
                tempstream.Write(BitConverter.GetBytes(Translation.y), 0, 8);
                tempstream.Write(BitConverter.GetBytes(Translation.z), 0, 8);
                tempstream.Write(BitConverter.GetBytes(Rotation.X), 0, 4);
                tempstream.Write(BitConverter.GetBytes(Rotation.Y), 0, 4);
                tempstream.Write(BitConverter.GetBytes(Rotation.Z), 0, 4);
                tempstream.Write(BitConverter.GetBytes(Rotation.W), 0, 4);
                tempstream.Write(BitConverter.GetBytes(PivotDistance), 0, 8);
                foreach (var value in InputView.Values)
                {
                    tempstream.Write(BitConverter.GetBytes(value), 0, 8);
                }
                var tempbuf = new byte[tempstream.Length];
                tempstream.Position = 0;
                tempstream.Read(tempbuf, 0, (int) tempstream.Length);
                tempstream.Dispose();
                return BitConverter.ToString(tempbuf);
            }
        }

        private string _prevViewChecksum = "";

        public string ProjectionChecksum
        {
            get
            {
                var tempstream = new MemoryStream();
                tempstream.Write(BitConverter.GetBytes(Fov), 0, 8);
                tempstream.Write(BitConverter.GetBytes(Near), 0, 8);
                tempstream.Write(BitConverter.GetBytes(Far), 0, 8);
                var tempbuf = new byte[tempstream.Length];
                tempstream.Position = 0;
                tempstream.Read(tempbuf, 0, (int)tempstream.Length);
                tempstream.Dispose();
                return BitConverter.ToString(tempbuf);
            }
        }

        private string _prevProjectionChecksum = "";

        public Matrix4x4 View
        {
            get
            {
                var chks = ViewChecksum;
                if (chks != _prevViewChecksum)
                {
                    _view = VMath.Translate(Translation) *
                            Matrix.RotationQuaternion(Rotation).ToMatrix4x4() *
                            VMath.Translate(0, 0, PivotDistance) * InputView;
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
                    _proj = VMath.PerspectiveLH(Fov, Near, Far, 1.0);
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
            var rotmat = new Matrix4x4(VMath.Inverse(View))
            {
                row4 = new Vector4D(0, 0, 0, 1)
            };
            if (delta.SetTranslation)
                Translation += rotmat * (delta.Translation * frametime);
            if (delta.SetRotation)
            {
                var inputrotmat = new Matrix4x4(InputView)
                {
                    row4 = new Vector4D(0, 0, 0, 1)
                };
                var inrotq = Quaternion.RotationMatrix(inputrotmat.ToSlimDXMatrix());
                var rottime = frametime * RotationSpeed;
                var rotq = Quaternion.RotationYawPitchRoll((float)(delta.PitchYawRoll.y * rottime), (float)(delta.PitchYawRoll.x * rottime), (float)(delta.PitchYawRoll.z * rottime));
                //Rotation = Quaternion.Normalize(inrotq * rotq * Quaternion.Invert(inrotq) * Rotation);
                Rotation = Quaternion.Normalize(Rotation * Quaternion.Invert(inrotq) * rotq * inrotq);
            }
            if (delta.SetPivotDistance)
                PivotDistance = Math.Max(0, PivotDistance + delta.PivotDistance * frametime);
            if (delta.SetFov)
            {
                var nfov = VMath.Map(Fov, 0.01, 0.45, 0, 1, TMapMode.Clamp);
                nfov += delta.Fov * frametime * (nfov + 0.05);
                Fov = VMath.Map(nfov, 0, 1, 0.01, 0.45, TMapMode.Clamp);
            }
            if (delta.SetNear)
                Near = Math.Max(0, Near + delta.Near * frametime);
            if (delta.SetFar)
                Far = Math.Max(0, Far + delta.Far * frametime);

            if (Default != null)
            {
                if (delta.ResetTranslation) Translation = new Vector3D(Default.Translation);
                if (delta.ResetRotation) Rotation = new Quaternion(Default.Rotation.X, Default.Rotation.Y, Default.Rotation.Z, Default.Rotation.W);
                if (delta.ResetPivotDistance) PivotDistance = Default.PivotDistance;
                if (delta.ResetFov) Fov = Default.Fov;
                if (delta.ResetNear) Near = Default.Near;
                if (delta.ResetFar) Far = Default.Far;
            }
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
                SetTranslation = true;
                _translation = value;
            }
        }

        public Vector3D PitchYawRoll
        {
            get => _pitchYawRoll;
            set
            {
                SetRotation = true;
                _pitchYawRoll = value;
            }
        }

        public double PivotDistance
        {
            get => _pivotDistance;
            set
            {
                SetPivotDistance = true;
                _pivotDistance = value;
            }
        }

        public double Fov
        {
            get => _fov;
            set
            {
                SetFov = true;
                _fov = value;
            }
        }

        public double Near
        {
            get => _near;
            set
            {
                SetNear = true;
                _near = value;
            }
        }

        public double Far
        {
            get => _far;
            set
            {
                SetFar = true;
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
