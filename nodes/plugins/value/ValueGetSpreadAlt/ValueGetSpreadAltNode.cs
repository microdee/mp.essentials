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
	#region PluginInfo
	[PluginInfo(Name = "GetSpreadAlt", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueGetSpreadAltNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		ISpread<double> FInput;
		[Input("Bin Size", DefaultValue = 0)]
		ISpread<int> FBin;
		[Input("Offset", DefaultValue = 0)]
		ISpread<int> FOffs;
		[Input("Count", DefaultValue = 1)]
		ISpread<int> FCount;

		[Output("Output")]
		ISpread<double> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			int countout = 0;
			for (int i=0; i<FCount.SliceCount; i++) countout+=FCount[i];
			FOutput.SliceCount = countout;
			int spreadcount = 0;
			int kk = 0;
			for (int i=0; i<FBin.SliceCount; i++)
			{
				if(FBin[i]!=0) {
					for (int j=0; j<FCount[i]; j++)
					{
						int mj = j%FBin[i] + spreadcount + FOffs[i];
						FOutput[kk] = FInput[mj];
						kk++;
						
					}
				}
				spreadcount += FBin[i];
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
