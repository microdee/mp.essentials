#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Values
{
	[PluginInfo(Name = "LFO",
	            Category = "Value",
            	Version = "Beat",
                Author = "microdee")]
	public class Template : IPluginEvaluate
	{
		[Import]
		public IHDEHost FHDEHost;
		#region fields & pins
		[Input("BPM", DefaultValue = 120)]
        public ISpread<double> FBPM;
        [Input("Tap", IsBang = true)]
        public ISpread<bool> FTap;
        [Input("Pause")]
        public ISpread<bool> FPause;
        [Input("Reset", IsBang = true)]
        public ISpread<bool> FReset;

        [Output("Output")]
        public ISpread<double> FOutput;

		[Import()]
        public ILogger FLogger;
		#endregion fields & pins

	    //private double currprog = 0;
	    private double pft = 0;
	    private bool init = true;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
		    if (init)
		    {
		        pft = FHDEHost.FrameTime;
		        init = false;
		    }

            FOutput.SliceCount = SpreadMax;
            double spf = FHDEHost.FrameTime - pft;
            pft = FHDEHost.FrameTime;
            for (int i = 0; i < SpreadMax; i++)
            {
                double spb = 0;
                if (FBPM[i] <= 0.0001) spb = 1;
                else spb = FBPM[i] / 60;
                FOutput[i] += FPause[i] ? 0 : spb * spf;
                if (FTap[i] && !FPause[i])
                {
                    FOutput[i] = Math.Round(FOutput[i], MidpointRounding.AwayFromZero);
                }
                if (FReset[i]) FOutput[i] = 0;
                //FOutput[0] = currprog;
            }
		}
	}
}
