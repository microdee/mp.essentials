#region usings
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
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "DragAndDrop", Category = "Control", Help = "Template with some gui elements", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	// UserControl,
	public class ControlDragAndDropNode : IPluginEvaluate
	{
		#region fields & pins
		//[Input("PosSize", DefaultValue = 0.0)]
		//ISpread<int> FPos;
		[Input("Input")]
		ISpread<System.Windows.Forms.Control> FInControl;

		[Output("Output", AllowFeedback=true)]
		ISpread<string> FOut;
		[Output("DragEnter", AllowFeedback=true, IsBang = true)]
		ISpread<bool> FDragEnter;
		[Output("DragInside", AllowFeedback=true)]
		ISpread<bool> FDragInside;
		[Output("DragLeave", AllowFeedback=true, IsBang = true)]
		ISpread<bool> FDragLeave;
		[Output("DragDrop", AllowFeedback=true, IsBang = true)]
		ISpread<bool> FDragDrop;

		[Import()]
		ILogger FLogger;
		
		int fcr;

		//gui controls
		//object FDragThreadLock = new object();
		bool pDragEnter = false;
		bool pDragInside = false;
		bool pDragLeave = false;
		bool pDragDrop = false;
		string[] pDragData;
		//= new string[] {""};
		#endregion fields & pins

		#region constructor and init

		[ImportingConstructor]
		public ControlDragAndDropNode()
		{
			fcr = 0;
		}

		void InitializeComponent()
		{
			FInControl[0].AllowDrop = true;

			//add a textbox
			FInControl[0].DragEnter += new System.Windows.Forms.DragEventHandler(_dragenter);
			FInControl[0].DragLeave += new EventHandler(_dragleave);
			FInControl[0].DragDrop += new System.Windows.Forms.DragEventHandler(_dragdrop);

			//add to controls
		}

		void _dragenter(object sender, System.Windows.Forms.DragEventArgs e)
		{
			/*if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
			else
				e.Effect = DragDropEffects.Link;*/
			pDragData = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			pDragEnter = true;
			pDragInside = true;
		}
		void _dragleave(object sender, EventArgs e)
		{
			pDragLeave = true;
			pDragInside = false;
		}
		void _dragdrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			//lock(FDragThreadLock)
			pDragDrop = true;
			pDragInside = false;
		}

		#endregion constructor and init

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(fcr==0) InitializeComponent();
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
			FDragDrop[0] = pDragDrop;
			FDragInside[0] = pDragInside;
			pDragEnter = false;
			pDragLeave = false;
			pDragDrop = false;
			//this.Location = new Point(FPos[0],FPos[1]);
			fcr++;
		}
	}
}
