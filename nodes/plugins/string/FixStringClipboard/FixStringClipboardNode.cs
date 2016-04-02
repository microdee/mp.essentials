#region usings
using System;
using System.Text;
using System.Windows.Forms;
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
	[PluginInfo(Name = "Clipboard", Category = "String", Version = "Fix", AutoEvaluate = true, Tags = "")]
	#endregion PluginInfo
	public class FixStringClipboardNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public ISpread<string> FInput;
		[Input("Set", IsBang = true)]
		public ISpread<bool> FSet;
		[Input("Get", IsBang = true)]
		public ISpread<bool> FGet;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FGet[0] && Clipboard.ContainsText())
				FOutput[0] = Clipboard.GetText();
			if(FSet[0])
				Clipboard.SetText(FInput[0]);

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
