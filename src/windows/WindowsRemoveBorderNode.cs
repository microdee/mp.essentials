#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;
using System.Drawing;
using System.Text;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "SetWindowBorder", Category = "Windows", AutoEvaluate = true, Tags = "")]
	#endregion PluginInfo
	public class WindowsSetWindowBorderNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Handle In", DefaultValue = 0)]
		public ISpread<int> FHandle;
		
		[Input("Border")]
		public ISpread<bool> FBorder;
		[Input("Caption")]
		public ISpread<bool> FCaption;
		[Input("Maximize Button")]
		public ISpread<bool> FMaxButt;
		[Input("Minimize Button")]
		public ISpread<bool> FMinButt;

		[Input("Set", IsBang = true)]
		public ISpread<bool> FSet;

		#endregion fields & pins

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern long GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);
        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool DrawMenuBar(IntPtr hWnd);
		
        private const int GWL_STYLE = -16;              //hex constant for style changing
        private const int WS_BORDER = 0x00800000;       //window with border
        private const int WS_CAPTION = 0x00C00000;      //window with a title bar
        private const int WS_SYSMENU = 0x00080000;      //window with no borders etc.
        private const int WS_MINIMIZEBOX = 0x00020000;  //window with minimizebox
        private const int WS_MAXIMIZEBOX = 0x00010000;  //window with maximizebox
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			for (int i = 0; i < FHandle.SliceCount; i++) {
				if (FSet[i]) {
					IntPtr w = new IntPtr(FHandle[0]);
					long Flags = 0;
					if(!FBorder[0]) Flags = Flags | WS_BORDER;
					if(!FCaption[0]) Flags = Flags | WS_CAPTION;
					if(!FMaxButt[0]) Flags = Flags | WS_MAXIMIZEBOX;
					if(!FMinButt[0]) Flags = Flags | WS_MINIMIZEBOX;
					Flags = ~Flags;
					long FixFlags = WS_BORDER | WS_CAPTION | WS_MAXIMIZEBOX | WS_MINIMIZEBOX;
					long OutFlags = (GetWindowLong(w, GWL_STYLE) | FixFlags) & Flags;
            		SetWindowLong(w, GWL_STYLE, OutFlags);
					DrawMenuBar(w);
				}
			}
		}
	}
}
