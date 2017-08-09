#region usings
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using mp.essentials;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Strings
{
	[PluginInfo(
        Name = "Find",
        Category = "String",
        Version = "Bin",
        Author = "microdee"
        )]
	public class BinStringFindNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<string> FInput;
		[Input("SubStrings")]
		public ISpread<ISpread<string>> FSubString;
	    [Input("Comparison Mode", DefaultEnumEntry = "InvariantCulture")]
	    public ISpread<StringComparison> FCompMode;
        [Input("Last")]
		public ISpread<bool> FLast;
	    [Input("Ignore Diacritics")]
	    public ISpread<bool> FIgnoreDiac;

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
                    if(FIgnoreDiac[i]) FFound[i][j] = FInput[i].RemoveDiacritics().Contains(FSubString[i][j].RemoveDiacritics(), FCompMode[i]);
                    else FFound[i][j] = FInput[i].Contains(FSubString[i][j], FCompMode[i]);
					if(FLast[i])
					{
					    if (FIgnoreDiac[i]) FPosition[i][j] = FInput[i].RemoveDiacritics().LastIndexOf(FSubString[i][j].RemoveDiacritics(), FCompMode[i]);
                        else FPosition[i][j] = FInput[i].LastIndexOf(FSubString[i][j], FCompMode[i]);
                    }
					else
					{
					    if (FIgnoreDiac[i]) FPosition[i][j] = FInput[i].RemoveDiacritics().IndexOf(FSubString[i][j].RemoveDiacritics(), FCompMode[i]);
					    else FPosition[i][j] = FInput[i].IndexOf(FSubString[i][j], FCompMode[i]);
                    }
				}
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
