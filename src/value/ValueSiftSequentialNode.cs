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
	[PluginInfo(
        Name = "SiftSequential",
        Category = "Value",
        Author = "microdee"
        )]
	public class ValueSiftSequentialNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<int> FInput;
		[Input("Filter")]
		public ISpread<int> FFilter;

		[Output("Output")]
		public ISpread<int> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FFilter.SliceCount;

			for (int i = 0; i < FFilter.SliceCount; i++)
			{
				int jj=-1;
				for(int j=0; j<FInput.SliceCount; j++)
				{
					if(FFilter[i] == FInput[j])
					{
						jj=j;
						break;
					}
				}
				FOutput[i] = jj;
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
