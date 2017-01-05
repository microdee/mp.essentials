using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
    [PluginInfo(
        Name = "VJoyFeeder",
        Category = "Devices",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class DevicesVJoyFeederNode : IPluginEvaluate
    {
        [Input("Joystick ID", MinValue = 1, MaxValue = 16, DefaultValue = 1)]
        public IDiffSpread<uint> FID;
        [Input("Axes")]
        public IDiffSpread<double> FAxesIn;
        [Input("Buttons")]
        public IDiffSpread<bool> FButtonsIn;
        [Input("POV Position")]
        public IDiffSpread<double> FPovPosIn;
        [Input("POV Set")]
        public IDiffSpread<bool> FPovSetIn;

        [Output("Enabled")]
        public ISpread<bool> FEnabled;
        [Output("Status")]
        public ISpread<string> FStatus;

        protected vJoy VJInstance;
        protected vJoy.JoystickState VJState;

        protected bool init = true;
        protected HID_USAGES[] Axes;
        protected bool[] AxisPresent;
        protected long[] AxisMaxVal;
        protected long[] AxisMinVal;
        protected int ButtonCount;
        protected int PovCount;
        protected VjdStat Status;

        public void Evaluate(int SpreadMax)
        {
            if (init)
            {
                VJInstance = new vJoy();
                VJState = new vJoy.JoystickState();
                if (!VJInstance.vJoyEnabled())
                {
                    FEnabled[0] = false;
                    return;
                }
                Status = VJInstance.GetVJDStatus(FID[0]);
                FStatus[0] = Status.ToString();

                ButtonCount = VJInstance.GetVJDButtonNumber(FID[0]);
                PovCount = VJInstance.GetVJDContPovNumber(FID[0]);
                if (Status != VjdStat.VJD_STAT_FREE && Status != VjdStat.VJD_STAT_OWN) return;
                if(!VJInstance.AcquireVJD(FID[0])) return;

                Axes = new[] {
                    HID_USAGES.HID_USAGE_X,
                    HID_USAGES.HID_USAGE_Y,
                    HID_USAGES.HID_USAGE_Z,
                    HID_USAGES.HID_USAGE_RX,
                    HID_USAGES.HID_USAGE_RY,
                    HID_USAGES.HID_USAGE_RZ,
                    HID_USAGES.HID_USAGE_SL0,
                    HID_USAGES.HID_USAGE_SL1,
                    HID_USAGES.HID_USAGE_WHL
                };
                AxisPresent = new bool[Axes.Length];
                AxisMaxVal = new long[Axes.Length];
                AxisMinVal = new long[Axes.Length];

                for (int i = 0; i < Axes.Length; i++)
                {
                    AxisPresent[i] = VJInstance.GetVJDAxisExist(FID[0], Axes[i]);
                    if (AxisPresent[i])
                    {
                        long minv = 0, maxv = 0;
                        VJInstance.GetVJDAxisMin(FID[0], Axes[i], ref minv);
                        VJInstance.GetVJDAxisMax(FID[0], Axes[i], ref maxv);
                        AxisMinVal[i] = minv;
                        AxisMaxVal[i] = maxv;
                    }
                }
                init = false;
            }
            FEnabled[0] = true;
            FStatus[0] = Status.ToString();
            for (int i = 0; i < Math.Min(FAxesIn.SliceCount, Axes.Length); i++)
            {
                if (AxisPresent[i])
                {
                    VJInstance.SetAxis(
                        (int)(FAxesIn[i]*AxisMaxVal[i] + (1 - FAxesIn[i])*AxisMinVal[i]),
                        FID[0],
                        Axes[i]);
                }
            }
            for (int i = 0; i < Math.Min(FButtonsIn.SliceCount, ButtonCount); i++)
            {
                VJInstance.SetBtn(FButtonsIn[i], FID[0], (uint)i + 1);
            }
            for (int i = 0; i < Math.Min(FPovPosIn.SliceCount, PovCount); i++)
            {
                if (!FPovSetIn[i]) VJInstance.SetContPov(-1, FID[0], (uint)i + 1);
                else VJInstance.SetContPov((int)(FPovPosIn[i] * 35999), FID[0], (uint)i + 1);
            }
        }
    }
}
