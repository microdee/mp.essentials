#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "PID", Category = "Windows", Version = "Handle", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class HandleWindowsPIDNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Handle In", DefaultValue = 0)]
		ISpread<int> FInput;

		[Output("ProcessID")]
		ISpread<uint> FPID;
		[Output("ThreadID")]
		ISpread<int> FTID;
		#endregion fields & pins

		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern IntPtr GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FPID.SliceCount = SpreadMax;
			FTID.SliceCount = SpreadMax;
			uint pid = 0;

			for (int i = 0; i < SpreadMax; i++)
			{
				FTID[i] = GetWindowThreadProcessId(new IntPtr(FInput[i]), out pid).ToInt32();
				FPID[i] = pid;
			}
		}
	}
}

