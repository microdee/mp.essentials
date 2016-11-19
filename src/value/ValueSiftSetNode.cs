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
	[PluginInfo(Name = "SiftSet", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueSiftSetNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<ISpread<double>> FInput;
		
		[Input("Filter")]
		public ISpread<double> FFilter;

		[Output("Index")]
		public ISpread<int> FOutput;
		[Output("Found")]
		public ISpread<bool> FFound;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			FFound.SliceCount = FInput.SliceCount;
			for(int i=0; i<FInput.SliceCount; i++)
			{
				int idx = 0;
				FFound[i] = false;
				for(int j=0; j<FInput[i].SliceCount; j++)
				{
					if(FInput[i][j] == FFilter[i])
					{
						idx = j;
						FFound[i] = true;
						break;
					}
				}
				FOutput[i] = idx;
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
