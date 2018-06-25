using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;

namespace mp.essentials.Nodes.Strings
{
	[PluginInfo(
        Name = "DragAndDrop",
        Category = "Control",
        Author = "microdee",
        AutoEvaluate = true
        )]
	public class ControlDragAndDropNode : IPluginEvaluate, IPluginFeedbackLoop, IPartImportsSatisfiedNotification
	{
		//[Input("PosSize", DefaultValue = 0.0)]
		//ISpread<int> FPos;
		[Input("Input")]
		public Pin<Control> FInControl;

		[Output("Output")]
		public ISpread<string> FOut;
		[Output("DragEnter", IsBang = true)]
		public ISpread<bool> FDragEnter;
		[Output("DragInside")]
		public ISpread<bool> FDragInside;
		[Output("DragLeave", IsBang = true)]
		public ISpread<bool> FDragLeave;

		[Import()]
		public ILogger FLogger;

		//gui controls
		//object FDragThreadLock = new object();
		bool pDragEnter = false;
		bool pDragInside = false;
		bool pDragLeave = false;
		bool pDragDrop = false;
		string[] pDragData;
		//= new string[] {""};

	    public void OnImportsSatisfied()
	    {
	        FInControl.Connected += (sender, args) =>
	        {
	            FInControl[0].AllowDrop = true;

	            FInControl[0].DragEnter += _dragenter;
	            FInControl[0].DragLeave += _dragleave;
	            FInControl[0].DragDrop += _dragdrop;
	        };
        }

		void _dragenter(object sender, DragEventArgs e)
		{
			pDragData = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			pDragEnter = true;
			pDragInside = true;
		}
		void _dragleave(object sender, EventArgs e)
		{
			pDragLeave = true;
			pDragInside = false;
		}
		void _dragdrop(object sender, DragEventArgs e)
		{
			pDragDrop = true;
			pDragInside = false;
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//set outputs
			FDragEnter[0] = pDragEnter;
			FDragLeave[0] = pDragEnter;
			if (pDragEnter) {
				FOut.SliceCount = pDragData.Length;
				int j = 0;
				foreach (string data in pDragData) {
					FOut[j] = data;
					j++;
				}
			}
			FDragLeave[0] = pDragLeave;
			FDragInside[0] = pDragInside;
			pDragEnter = false;
			pDragLeave = false;
			pDragDrop = false;
			//this.Location = new Point(FPos[0],FPos[1]);
		}

	    public bool OutputRequiresInputEvaluation(IPluginIO inputPin, IPluginIO outputPin)
	    {
	        return true;
	    }
	}
}
