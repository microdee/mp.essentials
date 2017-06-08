using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.essentials;
using mp.essentials.Camera;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
    [PluginInfo(
        Name = "CameraDelta",
        Category = "CameraDelta",
        Author = "microdee",
        Help = "Join node for patched camera controller"
    )]
    public class CameraDeltaNode : IPluginEvaluate
    {
        [Input("Delta In")]
        public Pin<CameraDelta> FDeltaIn;

        [Input("Translation", IsBang = true)]
        public ISpread<Vector3D> FTranslation;
        [Input("Pitch Yaw Roll", IsBang = true)]
        public ISpread<Vector3D> FRotation;
        [Input("Pivot Distance", IsBang = true)]
        public ISpread<double> FPivotDistance;
        [Input("Fov", IsBang = true)]
        public ISpread<double> FFov;

        [Input("Reset Translation", IsBang = true)]
        public ISpread<bool> FResetTranslation;
        [Input("Reset Pitch Yaw Roll", IsBang = true)]
        public ISpread<bool> FResetRotation;
        [Input("Reset Pivot Distance", IsBang = true)]
        public ISpread<bool> FResetPivotDistance;
        [Input("Reset Fov", IsBang = true)]
        public ISpread<bool> FResetFov;
        [Input("Lock Cursor")]
        public ISpread<bool> FLockCursor;

        [Output("Camera Delta Out")]
        public ISpread<CameraDelta> FCamDelta;

        private CameraDelta Delta = new CameraDelta();

        public void Evaluate(int SpreadMax)
        {
            if (FDeltaIn.IsConnected)
            {
                FCamDelta.SliceCount = FDeltaIn.SliceCount + 1;
                for (int i = 0; i < FDeltaIn.SliceCount; i++)
                {
                    FCamDelta[i] = FDeltaIn[i];
                }
                FCamDelta[-1] = Delta;
            }
            else FCamDelta[0] = Delta;
            Delta.ResetSignals();
            if (Delta.InputMouse != null)
            {
                if (FTranslation[0].Length > 0.00001) Delta.Translation = new Vector3D(FTranslation[0]);
                if (FRotation[0].Length > 0.00001) Delta.PitchYawRoll = new Vector3D(FRotation[0]);
                if (Math.Abs(FPivotDistance[0]) > 0.00001) Delta.PivotDistance = FPivotDistance[0];
                if (Math.Abs(FFov[0]) > 0.00001) Delta.Fov = FFov[0];
                Delta.ResetTranslation = FResetTranslation[0];
                Delta.ResetRotation = FResetRotation[0];
                Delta.ResetPivotDistance = FResetPivotDistance[0];
                Delta.ResetFov = FResetFov[0];
                Delta.LockCursor = FLockCursor[0];
            }
        }
    }
    [PluginInfo(
        Name = "UpstreamData",
        Category = "CameraDelta",
        Author = "microdee",
        Help = "Allows patchers to access upstream camera data without feedback"
    )]
    public class CameraDeltaUpstreamDataNode : IPluginEvaluate
    {
        [Input("Delta In")]
        public Pin<CameraDelta> FDeltaIn;

        [Output("Camera Delta Out")]
        public ISpread<CameraDelta> FCamDelta;

        private CameraDelta Delta = new CameraDelta();

        public void Evaluate(int SpreadMax)
        {
            if (FDeltaIn.IsConnected)
            {
                FCamDelta.SliceCount = FDeltaIn.SliceCount + 1;
                for (int i = 0; i < FDeltaIn.SliceCount; i++)
                {
                    FCamDelta[i] = FDeltaIn[i];
                }
                FCamDelta[-1] = Delta;
            }
            else FCamDelta[0] = Delta;
            Delta.ResetSignals();
        }
    }
}
