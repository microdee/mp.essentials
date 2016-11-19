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
	[PluginInfo(Name = "SpreadToFrames", Category = "Spreads")]
	public class SpreadsSpreadToFramesNode : SpreadToFrames<double> { }
	
	[PluginInfo(Name = "SpreadToFrames", Category = "String")]
	public class StringSpreadToFramesNode : SpreadToFrames<string> { }
	
	public abstract class SpreadToFrames<T> : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<T> FInput;
		[Input("Add", IsSingle=true, DefaultBoolean=false)]
		public ISpread<bool> FAdd;
		[Input("Delete", IsSingle=true, DefaultBoolean=true)]
		public ISpread<bool> FDelete;
		[Input("Maximum", IsSingle=true, DefaultValue=-1)]
		public ISpread<int> FMax;

		[Output("Output")]
		public ISpread<T> FOutput;

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
			if(FDelete[0] && FOutput.SliceCount > 0)
				FOutput.RemoveAt(0);
			if(FAdd[0])
			{
				for (int i = 0; i < FInput.SliceCount; i++)
				{
					if((FMax[0] > FOutput.SliceCount) || (FMax[0] < 0))
						FOutput.Add(FInput[i]);
				}
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
