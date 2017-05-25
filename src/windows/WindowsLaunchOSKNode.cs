#region usings
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices; //added for keyboard closure
using System.Windows.Interop;
using Microsoft.Win32;
//Keyboard closure - must add reference for WindowsBase

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    internal enum OSVersion
    {
        Undefined,
        Win7,
        Win8Or81,
        Win10
    }
    internal static class EnvironmentEx
    {
        private static OSVersion OSVersion = OSVersion.Undefined;

        internal static OSVersion GetOSVersion()
        {
            if (OSVersion != OSVersion.Undefined)
                return OSVersion;

            string OSName = GetOSName();

            if (OSName.Contains("7"))
                OSVersion = OSVersion.Win7;
            else if (OSName.Contains("8"))
                OSVersion = OSVersion.Win8Or81;
            else if (OSName.Contains("10"))
                OSVersion = OSVersion.Win10;

            return OSVersion;
        }

        private static string GetOSName()
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (rk == null) return "";
            return (string)rk.GetValue("ProductName");
        }
    }

    [PluginInfo(
        Name = "LaunchOSK",
        Category = "Windows",
        Author = "microdee",
        AutoEvaluate = true)]
	public class WindowsLaunchOSKNode : IPluginEvaluate
	{
	    private const string TabTipWindowClassName = "IPTip_Main_Window";
	    private const string TabTipExecPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
	    private const string TabTipRegistryKeyName = @"HKEY_CURRENT_USER\Software\Microsoft\TabletTip\1.7";

        #region fields & pins
        [Input("Open")]
		public ISpread<bool> FOpen;
		[Input("Close")]
		public ISpread<bool> FClose;

		[Output("Handle")]
		public ISpread<int> FHandle;
		
		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//Added for keyboard closure
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(String sClassName, String sAppName);

	    private static IntPtr GetTabTipWindowHandle() => FindWindow(TabTipWindowClassName, null);

        private static void EnableTabTipOpenInDesctopModeOnWin10()
	    {
	        const string TabTipAutoInvokeKey = "EnableDesktopModeAutoInvoke";

	        int EnableDesktopModeAutoInvoke = (int)(Registry.GetValue(TabTipRegistryKeyName, TabTipAutoInvokeKey, -1) ?? -1);
	        if (EnableDesktopModeAutoInvoke != 1)
	            Registry.SetValue(TabTipRegistryKeyName, TabTipAutoInvokeKey, 1);
	    }

        //open keyboard
        void openKeyboard()
        {
            const string TabTipDockedKey = "EdgeTargetDockedState";
            const string TabTipProcessName = "TabTip";

            int docked = (int)(Registry.GetValue(TabTipRegistryKeyName, TabTipDockedKey, 1) ?? 1);
            if (docked == 1)
            {
                Registry.SetValue(TabTipRegistryKeyName, TabTipDockedKey, 0);
                foreach (Process tabTipProcess in Process.GetProcessesByName(TabTipProcessName))
                    tabTipProcess.Kill();
            }

            if (EnvironmentEx.GetOSVersion() == OSVersion.Win10)
                EnableTabTipOpenInDesctopModeOnWin10();

            Process.Start(TabTipExecPath);
        }

		//close keyboard
		void closeKeyboard()
		{
		uint WM_SYSCOMMAND = 274;
            uint SC_CLOSE = 61536;
            IntPtr KeyboardWnd = FindWindow("IPTip_Main_Window", null);
            PostMessage(KeyboardWnd.ToInt32(), WM_SYSCOMMAND, (int)SC_CLOSE, 0);
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FOpen[0])
				openKeyboard();
			if(FClose[0])
				closeKeyboard();
            FHandle[0] = FindWindow("IPTip_Main_Window", null).ToInt32();

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
