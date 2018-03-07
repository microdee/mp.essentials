#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace mp.essentials.Nodes.Windows
{
	[PluginInfo(
        Name = "PID",
        Category = "Windows",
        Version = "Handle",
        Author = "microdee"
        )]
	public class HandleWindowsPIDNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Handle In", DefaultValue = 0)]
		public ISpread<int> FInput;

		[Output("ProcessID")]
		public ISpread<uint> FPID;
		[Output("ThreadID")]
		public ISpread<int> FTID;
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

