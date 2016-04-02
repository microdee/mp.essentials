#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Handle", Category = "Windows", Version = "PID", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class PIDWindowsHandleNode : IPluginEvaluate
	{
    	public delegate bool EnumDelegate(IntPtr hWnd, int lParam);
		
	    [DllImport("C:\\Windows\\System32\\user32.dll")]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    public static extern bool IsWindowVisible(IntPtr hWnd);
		
	    [DllImport("C:\\Windows\\System32\\user32.dll", EntryPoint = "EnumDesktopWindows",
	    ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
	    public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern IntPtr GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);
		
		#region fields & pins

		[Input("ProcessID")]
		public ISpread<uint> FPID;
		[Input("Check")]
		public ISpread<bool> FCheck;
		
		[Output("Handle", DefaultValue = 0)]
		public ISpread<ISpread<int>> FOut;
		#endregion fields & pins
		
		public Dictionary<uint, List<IntPtr>> ProcessHandlePairs = new Dictionary<uint, List<IntPtr>>();

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOut.SliceCount = FPID.SliceCount;

			for (int i = 0; i < FPID.SliceCount; i++)
			{
				
				EnumDelegate gethandles = delegate(IntPtr hWnd, int lParam)
				{
					uint pid = 0;
					GetWindowThreadProcessId(hWnd, out pid);
					if(this.ProcessHandlePairs.ContainsKey(pid))
					{
						this.ProcessHandlePairs[pid].Add(hWnd);
					}
					else
					{
						this.ProcessHandlePairs.Add(pid, new List<IntPtr>());
						this.ProcessHandlePairs[pid].Add(hWnd);
					}
				    return true;
				};
				
				if(FCheck[i])
				{
					this.ProcessHandlePairs.Clear();
					EnumDesktopWindows(IntPtr.Zero, gethandles, IntPtr.Zero);
					if(this.ProcessHandlePairs.ContainsKey(FPID[i]))
					{
						List<IntPtr> currl = this.ProcessHandlePairs[FPID[i]];
						FOut[i].SliceCount = currl.Count;
						for(int j = 0; j<currl.Count; j++)
						{
							FOut[i][j] = currl[j].ToInt32();
						}
					}
				}
			}
		}
	}
}

