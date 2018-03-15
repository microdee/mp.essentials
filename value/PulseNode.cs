using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Values
{
    public class Pulse
    {
        public Stopwatch Clock = new Stopwatch();
        public double Duration;
        public int Id;

        public Pulse()
        {
            Clock.Start();
        }
    }
    [PluginInfo(Name = "Pulse",
        Category = "Animation",
        Author = "microdee")]
    public class PulseNode : IPluginEvaluate
    {
        [Input("Trigger", IsBang = true)]
        public ISpread<bool> FTrig;
        [Input("Time")]
        public ISpread<double> FTime;
        [Input("Id")]
        public ISpread<int> FId;

        [Output("Pulses")]
        public ISpread<Pulse> FPulse;
        [Output("Output")]
        public ISpread<double> FOutput;
        [Output("Output Inverse")]
        public ISpread<double> FOutputInv;
        [Output("Id Out")]
        public ISpread<int> FIdOut;

        protected List<Pulse> Pulses = new List<Pulse>();

        private int autoid = 0;

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < SpreadMax; i++)
            {
                if (!FTrig[i]) continue;
                var id = FId[i] < 0 ? Math.Max(autoid, FId.Max() + 1) : FId[i];
                Pulses.Add(new Pulse
                {
                    Duration = FTime[i],
                    Id = id
                });
                autoid = id;
            }

            FPulse.AssignFrom(from pulse in Pulses where pulse.Clock.Elapsed.TotalSeconds <= pulse.Duration select pulse);
            FOutput.SliceCount = FOutputInv.SliceCount = FIdOut.SliceCount = FPulse.SliceCount;
            for (int i = 0; i < FPulse.SliceCount; i++)
            {
                FOutput[i] = FPulse[i].Clock.Elapsed.TotalSeconds / FPulse[i].Duration;
                FOutputInv[i] = 1.0 - FOutput[i];
                FIdOut[i] = FPulse[i].Id;
            }

            Pulses = FPulse.ToList();
        }
    }
}
