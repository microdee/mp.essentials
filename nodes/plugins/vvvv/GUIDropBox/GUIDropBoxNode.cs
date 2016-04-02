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
	[PluginInfo(Name = "DropBox", Category = "GUI", Help = "Template with some gui elements", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class GUIDropBoxNode : UserControl, IPluginEvaluate
	{
		#region fields & pins
		//[Input("PosSize", DefaultValue = 0.0)]
		//ISpread<int> FPos;
		
		[Output("Output")]
		ISpread<string> FOut;
		[Output("DragEnter")]
		ISpread<bool> FDragEnter;
		//[Output("DragLeave")]
		//ISpread<bool> FDragLeave;
		[Output("DragDrop")]
		ISpread<bool> FDragDrop;

		[Import()]
		ILogger FLogger;

		//gui controls
		//object FDragThreadLock = new object();
		bool pDragEnter = false;
		//bool pDragLeave = false;
		bool pDragDrop = false;
		string[] pDragData; //= new string[] {""};

		#endregion fields & pins

		#region constructor and init

		public GUIDropBoxNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			//clear controls in case init is called multiple times
			Controls.Clear();
			this.AllowDrop = true;

			//add a textbox
			this.DragEnter += new System.Windows.Forms.DragEventHandler(_dragenter);
			//this.DragLeave += new System.Windows.Forms.DragEventHandler(_dragleave);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(_dragdrop);
			
			//add to controls
		}

		void _dragenter(object sender, System.Windows.Forms.DragEventArgs e)
		{
    		if (e.Data.GetDataPresent(DataFormats.FileDrop))
        		e.Effect = DragDropEffects.Copy;
    		else
        		e.Effect = DragDropEffects.Link;
			//lock(FDragThreadLock)
			//{
				pDragData = (string[])e.Data.GetData(DataFormats.FileDrop, false);
				pDragEnter = true;
			//}
		}
		/*void _dragleave(object sender, System.Windows.Forms.DragEventArgs e)
		{
			//lock(FDragThreadLock)
				pDragLeave = true;
		}*/
		void _dragdrop(object sender, System.Windows.Forms.DragEventArgs e)
		{
			//lock(FDragThreadLock)
				pDragDrop = true;
		}

		#endregion constructor and init

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//set outputs
			FDragEnter[0] = pDragEnter;
			if(pDragEnter)
			{
				FOut.SliceCount = pDragData.Length;
				int j = 0;
				foreach (string data in pDragData)
				{
					FOut[j] = data;
					j++;
				}
			}
			pDragEnter = false;
			//FDragLeave[0] = pDragLeave;
			//pDragLeave = false;
			FDragDrop[0] = pDragDrop;
			pDragDrop = false;
			//this.Location = new Point(FPos[0],FPos[1]);
		}
	}
}
