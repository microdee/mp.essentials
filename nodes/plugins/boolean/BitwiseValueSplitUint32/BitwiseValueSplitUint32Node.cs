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
	[PluginInfo(Name = "SplitUint32", Category = "Value", Version = "Bitwise", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class BitwiseValueSplitUint32Node : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public ISpread<uint> FInput;

		[Output("Output")]
		public ISpread<ISpread<bool>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;

			for (int i = 0; i < FOutput.SliceCount; i++) {
				FOutput[i].SliceCount = 32;
				for (int j = 0; j < 32; j++)
				{
					uint mask = 0x80000000; 
					mask = mask >> j;
					uint masked = FInput[i] & mask;
					FOutput[i][j] = masked != 0;
				}
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "JoinUint32", Category = "Value", Version = "Bitwise", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class BitwiseValueJoinUint32Node : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<ISpread<bool>> FInput;

		[Output("Output")]
		public ISpread<uint> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;

			for (int i = 0; i < FOutput.SliceCount; i++) {
				FOutput[i] = 0;
				for (int j = 0; j < FInput[i].SliceCount; j++)
				{
					uint mask = 0x80000000;
					mask = mask >> (32 - FInput[i].SliceCount);
					mask = mask >> j;
					if(FInput[i][j]) FOutput[i] = FOutput[i] | mask;
				}
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
