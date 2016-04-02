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
	[PluginInfo(Name = "Router", Category = "String", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class StringRouterNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<ISpread<string>> FInput;
		[Input("Type")]
		public ISpread<ISpread<string>> FType;
		
		[Input("Unique Type")]
		public ISpread<string> FUType;

		[Output("Output")]
		public ISpread<ISpread<string>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			for(int i=0; i<FInput.SliceCount; i++)
			{
				FOutput[i].SliceCount = FUType.SliceCount;
				for(int j=0; j<FUType.SliceCount; j++)
				{
					for(int k=0; k<FType[i].SliceCount; k++)
					{
						if(FType[i][k] == FUType[j])
						{
							FOutput[i][j] = FInput[i][k];
						}
					}
				}
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
