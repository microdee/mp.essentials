#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Find", Category = "String", Version = "Bin", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class BinStringFindNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<string> FInput;
		[Input("SubStrings")]
		public ISpread<ISpread<string>> FSubString;
		[Input("Last")]
		public ISpread<bool> FLast;

		[Output("Found")]
		public ISpread<ISpread<bool>> FFound;
		[Output("Start")]
		public ISpread<ISpread<int>> FPosition;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FFound.SliceCount = Math.Max(FInput.SliceCount, FSubString.SliceCount);
			FPosition.SliceCount = FFound.SliceCount;

			for (int i = 0; i < FFound.SliceCount; i++)
			{
				FFound[i].SliceCount = FSubString[i].SliceCount;
				FPosition[i].SliceCount = FFound[i].SliceCount;
				
				for(int j=0; j<FFound[i].SliceCount; j++)
				{
					FFound[i][j] = FInput[i].Contains(FSubString[i][j]);
					if(FLast[i])
					{
						FPosition[i][j] = FInput[i].LastIndexOf(FSubString[i][j]);
					}
					else
					{
						FPosition[i][j] = FInput[i].IndexOf(FSubString[i][j]);
					}
				}
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
