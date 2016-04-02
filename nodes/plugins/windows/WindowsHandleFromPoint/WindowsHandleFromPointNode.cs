#region usings
using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "HandleFromPoint", Category = "Windows", Tags = "")]
	#endregion PluginInfo
	public class WindowsHandleFromPointNode : IPluginEvaluate
	{		
		[Input("Update", DefaultBoolean = true)]
		ISpread<bool> FUpdate;
		
		[Output("Handle Out")]
		ISpread<int> FParent;
		[Output("Cursor Pos")]
		ISpread<int> FCurPos;
		
		[Output("Title")]
		ISpread<string> FTitle;
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern bool GetCursorPos(out Point lpPoint);
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern IntPtr WindowFromPoint(Point lpPoint);
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern IntPtr ChildWindowFromPoint(IntPtr hWndParent, Point Point);
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FUpdate[0])
			{
				FParent.SliceCount = 1;
				FTitle.SliceCount = 1;
				FCurPos.SliceCount = 2;
				const int nChars = 256;
				
				Point ptCursor = new Point();
				GetCursorPos(out ptCursor);
				FCurPos[0] = ptCursor.X;
				FCurPos[1] = ptCursor.Y;
				
				IntPtr Parent = WindowFromPoint(ptCursor);
				IntPtr Child = ChildWindowFromPoint(Parent, ptCursor);
				StringBuilder Buff = new StringBuilder(nChars);
				if(GetWindowText(Parent, Buff, nChars) > 0) FTitle[0] = Buff.ToString();
				else FTitle[0] = "";
				
				FParent[0] = Parent.ToInt32();
			}
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "SetCursorPos", Category = "Windows", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class WindowsSetCursorPosNode : IPluginEvaluate
	{		
		[Input("Cursor Pos")]
		ISpread<int> FCurPos;
		[Input("Set")]
		ISpread<bool> FSet;
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern bool SetCursorPos(int X, int Y);

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FSet[0]) SetCursorPos(FCurPos[0],FCurPos[1]);
		}
	}
}