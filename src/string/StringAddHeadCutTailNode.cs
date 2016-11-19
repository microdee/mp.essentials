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
	[PluginInfo(Name = "AddHeadCutTail", Category = "String", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class StringAddHeadCutTailNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public ISpread<string> FInput;
		[Input("Add Head", IsBang = true)]
		public ISpread<bool> FAH;
		[Input("Cut Tail", IsBang = true)]
		public ISpread<bool> FCT;
		[Input("Reset", IsBang = true)]
		public ISpread<bool> FReset;
		[Input("Filter")]
		public ISpread<bool> FFilter;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FOutput.SliceCount = 0;
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FAH[0]) 
			{
				if(FFilter[0])
				{
					if(!FOutput.Contains(FInput[0]))
						FOutput.Insert(0, FInput[0]);
				}
				else FOutput.Insert(0, FInput[0]);
			}
			if(FCT[0])
			{
				FOutput.RemoveAt(FOutput.SliceCount-1);
			}
			if(FReset[0]) FOutput.SliceCount = 0;
			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
