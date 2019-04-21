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

        [Input("Vector Size", MinValue = 1, DefaultValue = 1)]
        public ISpread<int> FVectorSize;

        [Input("Start")]
		public ISpread<ISpread<double>> FStart;
		
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
                        for(int vi = 0; vi < FVectorSize[0]; vi++)
                            FOutput[i][vi] = FStart[i][vi];

						for (int j = FVectorSize[0]; j < FInput[i].SliceCount; j++)
                        {
                            FOutput[i][j] = FOutput[i][j-FVectorSize[0]] + FInput[i][j-FVectorSize[0]] *FMul[i];
						}
					}
				}
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
