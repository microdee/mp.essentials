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
        [Output("FrameTime", Order = 4)]
        public ISpread<double> FFrameTime;

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

        private ModularCam Camera;
        private WPoint CursorPos;
        private double PrevFrameTime;

        private MouseInputManager MouseMan = new MouseInputManager();
        private KeyboardInputManager KeyMan = new KeyboardInputManager();

        public void Evaluate(int SpreadMax)
        {
            if (FKeyboard.IsChanged || FMouse.IsChanged)
            {
                MouseMan.SelectDevice(FMouse[0]);
                KeyMan.SelectDevice(FKeyboard[0]);
            }
            if (Camera == null)
            {
                var qrot = Quaternion.RotationYawPitchRoll((float)(FDefRot[0].y * Math.PI * 2), (float)(FDefRot[0].x * Math.PI * 2), (float)(FDefRot[0].z * Math.PI * 2));
                Camera = new ModularCam()
                {
                    Default = new ModularCam()
                    {
                        Translation = FDefTrans[0],
                        Rotation = qrot,
                        PivotDistance = FDefPivotDist[0],
                        Fov = FDefFov[0],
                        Near = FDefNear[0],
                        Far = FDefFar[0]
                    },
                    Translation = FDefTrans[0],
                    Rotation = qrot,
                    PivotDistance = FDefPivotDist[0],
                    Fov = FDefFov[0],
                    Near = FDefNear[0],
                    Far = FDefFar[0],
                    RotationSpeed = FRotSpeed[0]
                };
            }
            Camera.InputView = FTrIn[0];
            Camera.InputAspect = FAspectIn[0];
            if (FRotSpeed.IsChanged || FResetAll[0]) Camera.RotationSpeed = FRotSpeed[0];
            if (FDefTrans.IsChanged || FResetAll[0])
            {
                Camera.Translation = FDefTrans[0];
                Camera.Default.Translation = FDefTrans[0];
            }
            if (FDefRot.IsChanged || FResetAll[0])
            {
                var qrot = Quaternion.RotationYawPitchRoll((float)(FDefRot[0].y * Math.PI * 2), (float)(FDefRot[0].x * Math.PI * 2), (float)(FDefRot[0].z * Math.PI * 2));
                Camera.Rotation = qrot;
                Camera.Default.Rotation = qrot;
            }
            if (FDefPivotDist.IsChanged || FResetAll[0])
            {
                Camera.PivotDistance = FDefPivotDist[0];
                Camera.Default.PivotDistance = FDefPivotDist[0];
            }
            if (FDefFov.IsChanged || FResetAll[0])
            {
                Camera.Fov = FDefFov[0];
                Camera.Default.Fov = FDefFov[0];
            }
            if (FDefNear.IsChanged || FResetAll[0])
            {
                Camera.Near = FDefNear[0];
                Camera.Default.Near = FDefNear[0];
            }
            if (FDefFar.IsChanged || FResetAll[0])
            {
                Camera.Far = FDefFar[0];
                Camera.Default.Far = FDefFar[0];
            }
            if (FDeltas.SliceCount > 0 && FDeltas[0] != null)
            {
                if (FDeltas.Any(delta => delta.LockCursor))
                {
                    SetCursorPos(CursorPos.X, CursorPos.Y);
                }
                else
                {
                    GetCursorPos(out CursorPos);
                }
                var hoverhandle = WindowFromPoint(CursorPos);
                var parenthandle = GetParent((IntPtr) FHandle[0]);
                for (int i = 0; i < FDeltas.SliceCount; i++)
                {
                    var delta = FDeltas[i];
                    delta.InputMouse = MouseMan.Devices[0];
                    delta.InputKeyboard = KeyMan.Devices[0];
                    delta.ConnectedCamera = Camera;
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
                    FFrameTime[0] = FHDEHost.FrameTime - PrevFrameTime;
                    Camera.Move(delta, FHDEHost.FrameTime - PrevFrameTime);
                }
            }
            FCameraOut[0] = Camera;
            FViewOut[0] = Camera.View;
            FProjectionOut[0] = Camera.Projection;
            FProjectionWithAspectOut[0] = Camera.ProjectionWithAspect;
            PrevFrameTime = FHDEHost.FrameTime;
        }

        public bool OutputRequiresInputEvaluation(IPluginIO inputPin, IPluginIO outputPin)
        {
            return !inputPin.Name.Contains("Default") &&
                inputPin.Name != "Transform In" &&
                inputPin.Name != "Camera Delta";
        }
    }
}
