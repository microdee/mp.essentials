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
	public struct LPRECT {
		public long left;
		public long top;
		public long right;
		public long bottom;
	}
	
	public enum WindowPosFlags {
		None = 0x0000,
		SWP_ASYNCWINDOWPOS = 0x4000,
		SWP_DEFERERASE = 0x2000,
		SWP_DRAWFRAME = 0x0020,
		SWP_FRAMECHANGED = 0x0020,
		SWP_HIDEWINDOW = 0x0080,
		SWP_NOACTIVATE = 0x0010,
		SWP_NOCOPYBITS = 0x0100,
		SWP_NOMOVE = 0x0002,
		SWP_NOOWNERZORDER = 0x0200,
		SWP_NOREDRAW = 0x0008,
		SWP_NOSENDCHANGING = 0x0400,
		SWP_NOSIZE = 0x0001,
		SWP_NOZORDER = 0x0004,
		SWP_SHOWWINDOW = 0x0040
	}
	
	public enum WindowPosZOrder {
		Bottom,
		NoTopMost,
		Top,
		TopMost
	}
	#region PluginInfo
	[PluginInfo(Name = "WindowPos", Category = "Windows", AutoEvaluate = true, Tags = "")]
	#endregion PluginInfo
	public class WindowsWindowPosNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Handle In", DefaultValue = 0)]
		public ISpread<int> FHandle;
		
		[Input("Position", DefaultValue = 0)]
		public ISpread<Vector2D> FPos;
		[Input("Width", DefaultValue = 0)]
		public ISpread<Vector2D> FSize;
		
		[Input("Z Order")]
		public ISpread<WindowPosZOrder> FZOrder;
		[Input("Flags")]
		public ISpread<WindowPosFlags> FFlags;
		
		[Input("Set")]
		public ISpread<bool> FSet;

		[Output("Position Out")]
		public ISpread<Vector2D> FPosOut;
		[Output("Width Out")]
		public ISpread<Vector2D> FSizeOut;
		[Output("Result")]
		public ISpread<bool> FResult;
		
		#endregion fields & pins

		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		Rectangle outrect = new Rectangle();
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FPosOut.SliceCount = FHandle.SliceCount;
			FSizeOut.SliceCount = FHandle.SliceCount;
			FResult.SliceCount = FHandle.SliceCount;
			
			IntPtr zorder = (IntPtr)0;
			uint flags = 0;
			
			for (int i = 0; i < FFlags.SliceCount; i++)
			{
				flags = flags | (uint)FFlags[i];
			}

			for (int i = 0; i < FHandle.SliceCount; i++)
			{
				if(FSet[i])
				{
					int tx = (int)FPos[i].x;
					int ty = (int)FPos[i].y;
					int tcx = (int)FSize[i].x;
					int tcy = (int)FSize[i].y;
					
					if(FZOrder[i] == WindowPosZOrder.Bottom) zorder = (IntPtr)1;
					if(FZOrder[i] == WindowPosZOrder.NoTopMost) zorder = (IntPtr)(-2);
					if(FZOrder[i] == WindowPosZOrder.Top) zorder = (IntPtr)0;
					if(FZOrder[i] == WindowPosZOrder.TopMost) zorder = (IntPtr)(-1);
					FResult[i] = SetWindowPos((IntPtr)FHandle[i], zorder, tx, ty, tcx, tcy, flags);
				}
				GetWindowRect((IntPtr)FHandle[i], out outrect);
				Vector2D opos = new Vector2D((double)outrect.Left, (double)outrect.Top);
				FPosOut[i] = opos;
				Vector2D osize = new Vector2D((double)outrect.Width - (double)outrect.Left, (double)outrect.Height - (double)outrect.Top);
				FSizeOut[i] = osize;
			}
		}
	}
}
