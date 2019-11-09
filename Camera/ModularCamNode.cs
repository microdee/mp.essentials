using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using md.stdl.Interaction;
using mp.essentials.Camera;
using mp.pddn;
using SlimDX;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;
using WPoint = System.Drawing.Point;

namespace mp.essentials.Nodes.Camera
{
    [PluginInfo(
        Name = "CameraDelta",
        Category = "CameraDelta",
        Version = "Split",
        Author = "microdee"
    )]
    public class CameraDeltaSplitNode : ObjectSplitNode<CameraDelta> { }

    [PluginInfo(
        Name = "Camera",
        Category = "ModularCam",
        Version = "Split",
        Author = "microdee"
    )]
    public class ModularCamSplitNode : ObjectSplitNode<ModularCam> { }

    [PluginInfo(
        Name = "Camera",
        Category = "Transform",
        Version = "Modular",
        Author = "microdee"
    )]
    public class ModularTransformCameraNode : IPluginEvaluate, IPluginFeedbackLoop
    {
        [Import] public IHDEHost FHDEHost;

        [Input("Transform In")]
        public ISpread<Matrix4x4> FTrIn;
        [Input("Camera Delta")]
        public ISpread<CameraDelta> FDeltas;

        [Input("Default Translation")]
        public IDiffSpread<Vector3D> FDefTrans;
        [Input("Default Rotation", DefaultValues = new [] {0.0, 0.0, 0.0, 1.0})]
        public IDiffSpread<Vector3D> FDefRot;
        [Input("Rotation Speed", DefaultValue = 2)]
        public IDiffSpread<double> FRotSpeed;
        [Input("Default Pivot Distance")]
        public IDiffSpread<double> FDefPivotDist;
        [Input("Default FOV", DefaultValue = 0.25)]
        public IDiffSpread<double> FDefFov;
        [Input("Default Near", DefaultValue = 0.1)]
        public IDiffSpread<double> FDefNear;
        [Input("Default Far", DefaultValue = 100)]
        public IDiffSpread<double> FDefFar;

        [Input("Mouse ID", DefaultValue = -1)]
        public ISpread<int> FMouse;
        [Input("Keyboard ID", DefaultValue = -1)]
        public ISpread<int> FKeyboard;
        [Input("Target Window Handle", DefaultValue = -1)]
        public ISpread<int> FHandle;

        [Input("Reset All", IsBang = true)]
        public ISpread<bool> FResetAll;

        [Input("Aspect Ratio In")]
        public ISpread<Matrix4x4> FAspectIn;

        [Output("View", Order = 0)]
        public ISpread<Matrix4x4> FViewOut;
        [Output("Projection", Order = 1)]
        public ISpread<Matrix4x4> FProjectionOut;
        [Output("Projection With Aspect Ratio", Order = 2)]
        public ISpread<Matrix4x4> FProjectionWithAspectOut;
        [Output("Camera", Order = 3)]
        public ISpread<ModularCam> FCameraOut;

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool GetCursorPos(out WPoint lpPoint);

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern IntPtr WindowFromPoint(WPoint lpPoint);

        private CameraDelta _resetter;
        private ModularCam _camera;
        private WPoint _cursorPos;
        private double _prevFrameTime;

        private readonly MouseInputManager _mouseMan = new MouseInputManager();
        private readonly KeyboardInputManager _keyMan = new KeyboardInputManager();

        public void Evaluate(int SpreadMax)
        {
            var qrot = Quaternion.RotationYawPitchRoll(
                (float)(FDefRot[0].y * Math.PI * 2),
                (float)(FDefRot[0].x * Math.PI * 2),
                (float)(FDefRot[0].z * Math.PI * 2)
            );
            var defcam = new CameraProperties(
                FTrIn[0],
                FDefTrans[0],
                qrot,
                FDefPivotDist[0],
                FDefFov[0],
                FDefNear[0],
                FDefFar[0]
            );

            if (FKeyboard.IsChanged || FMouse.IsChanged)
            {
                _mouseMan.SelectDevice(FMouse[0]);
                _keyMan.SelectDevice(FKeyboard[0]);
            }
            if (_camera == null)
            {
                _camera = new ModularCam()
                {
                    Default = defcam,
                    Properties = defcam,
                    RotationSpeed = FRotSpeed[0]
                };
            }
            if(_resetter == null) _resetter = new CameraDelta();

            _resetter.ResetTranslation = FDefTrans.IsChanged || FResetAll[0];
            _resetter.ResetRotation = FDefRot.IsChanged || FResetAll[0];
            _resetter.ResetPivotDistance = FDefPivotDist.IsChanged || FResetAll[0];
            _resetter.ResetFov = FDefFov.IsChanged || FResetAll[0];
            _resetter.ResetNear = FDefNear.IsChanged || FResetAll[0];
            _resetter.ResetFar = FDefFar.IsChanged || FResetAll[0];

            _camera.InputAspect = FAspectIn[0];
            if (FRotSpeed.IsChanged || FResetAll[0]) _camera.RotationSpeed = FRotSpeed[0];

            _camera.Default = defcam;
            _camera.Properties = new CameraProperties(_camera.Properties, defcam, _resetter);

            if (FDeltas.SliceCount > 0 && FDeltas[0] != null)
            {
                if (FDeltas.Any(delta => delta.LockCursor))
                {
                    SetCursorPos(_cursorPos.X, _cursorPos.Y);
                }
                else
                {
                    GetCursorPos(out _cursorPos);
                }
                var hoverhandle = WindowFromPoint(_cursorPos);
                var parenthandle = GetParent((IntPtr) FHandle[0]);
                for (int i = 0; i < FDeltas.SliceCount; i++)
                {
                    var delta = FDeltas[i];
                    delta.InputMouse = _mouseMan.Devices[0];
                    delta.InputKeyboard = _keyMan.Devices[0];
                    delta.ConnectedCamera = _camera;
                    if (FHandle[0] > 0)
                    {
                        delta.InteractUpstream =
                            FHandle[0] == hoverhandle.ToInt32() ||
                            parenthandle == hoverhandle;
                    }
                    else
                    {
                        delta.InteractUpstream = true;
                    }
                    _camera.Move(delta, FHDEHost.FrameTime - _prevFrameTime);
                }
            }
            FCameraOut[0] = _camera;
            FViewOut[0] = _camera.View;
            FProjectionOut[0] = _camera.Projection;
            FProjectionWithAspectOut[0] = _camera.ProjectionWithAspect;
            _prevFrameTime = FHDEHost.FrameTime;
        }

        public bool OutputRequiresInputEvaluation(IPluginIO inputPin, IPluginIO outputPin)
        {
            return !inputPin.Name.Contains("Default") &&
                inputPin.Name != "Transform In" &&
                inputPin.Name != "Camera Delta";
        }
    }
}
