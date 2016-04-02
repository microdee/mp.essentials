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
	[PluginInfo(Name = "CountBangs", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueCountBangsNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", IsBang = true)]
		public ISpread<bool> FInput;
		[Input("Reset", IsBang = true)]
		public ISpread<bool> FReset;

		[Output("Output")]
		public ISpread<int> FOutput;
		
		public int Counter;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			bool valid = false;
			for (int i = 0; i < SpreadMax; i++)
			{
				if(FInput[i])
				{
					valid = true;
					break;
				}
			}
			if(valid)
			{
				FOutput.SliceCount = SpreadMax;
				for (int i = 0; i < SpreadMax; i++)
				{
					Counter += (FInput[i]) ? 1 : 0;
					FOutput[i] = Counter;
				}
			}
			if(FReset[0])
			{
				FOutput.SliceCount = 1;
				FOutput[0] = 0;
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
