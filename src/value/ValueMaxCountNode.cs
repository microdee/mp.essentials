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
	[PluginInfo(Name = "MaxCount", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueMaxCountNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Node", IsPinGroup = true)]
		public ISpread<ISpread<object>> FNode;
		[Input("String", IsPinGroup = true)]
		public ISpread<ISpread<string>> FString;
		[Input("Value", IsPinGroup = true)]
		public ISpread<ISpread<double>> FValue;
		[Input("Color", IsPinGroup = true)]
		public ISpread<ISpread<RGBAColor>> FColor;
		[Input("Enumeration", IsPinGroup = true)]
		public ISpread<ISpread<EnumEntry>> FEnum;

		[Output("Output")]
		public ISpread<double> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = 1;
			FOutput[0] = 0;
			foreach(ISpread<object> s in FNode)
				FOutput[0] = Math.Max(FOutput[0], s.SliceCount);
			foreach(ISpread<string> s in FString)
				FOutput[0] = Math.Max(FOutput[0], s.SliceCount);
			foreach(ISpread<double> s in FValue)
				FOutput[0] = Math.Max(FOutput[0], s.SliceCount);
			foreach(ISpread<RGBAColor> s in FColor)
				FOutput[0] = Math.Max(FOutput[0], s.SliceCount);
			foreach(ISpread<EnumEntry> s in FEnum)
				FOutput[0] = Math.Max(FOutput[0], s.SliceCount);
		}
	}
}
