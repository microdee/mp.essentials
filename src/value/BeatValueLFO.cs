#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
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
        [Input("Reset", IsBang = true)]
        public ISpread<bool> FReset;

        [Output("Output")]
        public ISpread<double> FOutput;

		[Import()]
        public ILogger FLogger;
		#endregion fields & pins

	    private double currprog = 0;
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
            double spf = FHDEHost.FrameTime - pft;
		    pft = FHDEHost.FrameTime;
		    double spb = 0;
		    if (FBPM[0] <= 0.0001) spb = 1;
		    else spb = FBPM[0]/60;
		    currprog += spb * spf;
		    if (FTap[0])
		    {
		        currprog = Math.Round(currprog, MidpointRounding.AwayFromZero);
		    }
		    if (FReset[0]) currprog = 0;
		    FOutput[0] = currprog;
		}
	}
}
