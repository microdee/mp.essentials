#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;
using System.Windows.Forms;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace mp.essentials.Nodes.Windows
{
	[PluginInfo(
        Name = "SendKey",
        Category = "Windows",
        Author = "microdee",
        AutoEvaluate = true
        )]
	public class WindowsSendKeys : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		ISpread<string> FInput;

	    [Input("Send", IsBang = true)]
	    ISpread<bool> FSend;
        #endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
		    if (FSend[0])
		    {
		        SendKeys.Send(FInput[0]);
		    }
		}
	}
}
