#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;
using md.stdl.String;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Strings
{
	[PluginInfo(
        Name = "Edit",
        Category = "String",
        Author = "microdee"
        )]
	public class StringEditNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public ISpread<string> FInput;
		[Input("Position")]
		public ISpread<ISpread<int>> FPos;
		[Input("Length")]
		public ISpread<ISpread<int>> FLength;
		[Input("Insert")]
		public ISpread<ISpread<string>> FInsert;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
		    FOutput.SliceCount = FInput.SliceCount;
			for (int i = 0; i < FInput.SliceCount; i++)
			{
			    var sm = new [] {FPos[i].SliceCount, FLength[i].SliceCount, FInsert[i].SliceCount}.Max();
			    FOutput[i] = FInput[i];
                int offs = 0;
                for (int j = 0; j < sm; j++)
			    {
			        var diff = FInsert[i][j].Length - FLength[i][j];
			        var pos = FPos[i][j] + offs;
                    FOutput[i] = FOutput[i].Remove(pos, FLength[i][j]);
			        FOutput[i] = FOutput[i].Insert(pos, FInsert[i][j]);
			        offs += diff;
			    }
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
