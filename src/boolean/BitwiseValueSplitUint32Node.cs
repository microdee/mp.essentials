#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;
using md.stdl.Boolean;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Boolean
{
	[PluginInfo(
        Name = "SplitUint32",
        Category = "Value",
        Version = "Bitwise",
        Author = "microdee"
        )]
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
                FOutput[i].AssignFrom(BitUtils.Split(FInput[i]));
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
	
	[PluginInfo(
        Name = "JoinUint32",
        Category = "Value",
        Version = "Bitwise",
        Author = "microdee"
        )]
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
				FOutput[i] = BitUtils.Join(FInput[i].ToArray());
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
