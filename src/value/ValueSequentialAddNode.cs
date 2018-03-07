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
        Name = "SequentialAdd",
        Category = "Value",
        Author = "microdee"
        )]
	public class ValueSequentialAddNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public ISpread<ISpread<double>> FInput;
		
		[Input("Start")]
		public ISpread<double> FStart;
		
		[Input("Multiplier", DefaultValue = 1.0)]
		public ISpread<double> FMul;

		[Output("Output")]
		public ISpread<ISpread<double>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			if(FInput.SliceCount!=0)
			{
				for (int i = 0; i < FInput.SliceCount; i++)
				{
					FOutput[i].SliceCount = FInput[i].SliceCount;
					if(FInput[i].SliceCount!=0)
					{
						FOutput[i][0] = FStart[i];
						for (int j = 1; j < FInput[i].SliceCount; j++)
						{
							FOutput[i][j] = FOutput[i][j-1] + FInput[i][j-1]*FMul[i];
						}
					}
				}
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
