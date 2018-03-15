#region usings
using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Text.RegularExpressions;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Strings
{
	[PluginInfo(
        Name = "RegexSplit",
        Category = "String",
        Author = "microdee"
        )]
	public class StringSeparateFixNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public IDiffSpread<string> FInput;
		[Input("Regex")]
		public IDiffSpread<string> FPattern;

		[Output("Output")]
		public ISpread<ISpread<string>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;

			if(FInput.IsChanged || FPattern.IsChanged)
			{
				for (int i = 0; i < FInput.SliceCount; i++)
				{
					string[] result = Regex.Split(FInput[i], FPattern[i]);
					FOutput[i].SliceCount = result.Length;
					for(int j=0; j<result.Length; j++)
					{
						FOutput[i][j] = result[j];
					}
				}
					
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
