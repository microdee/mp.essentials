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

namespace mp.essentials.Nodes.vvvv
{
	#region PluginInfo
	[PluginInfo(
        Name = "TextBox",
        Category = "GUI",
        Author = "microdee",
        AutoEvaluate = true
        )]
	#endregion PluginInfo
	public class GUITextBoxNode : UserControl, IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", IsSingle = true)]
		public ISpread<string> FIn;
		[Input("Set", IsSingle = true)]
		public ISpread<bool> FSet;
		[Input("Select All", IsSingle = true, IsBang = true)]
		public ISpread<bool> FSelectAll;
	    [Input("Selection Start In")]
	    public ISpread<int> FSelStartIn;
	    [Input("Selection Length In")]
	    public ISpread<int> FSelLengthIn;
	    [Input("Select", IsSingle = true, IsBang = true)]
	    public ISpread<bool> FSelect;

        [Output("Output")]
        public ISpread<string> FOut;
		[Output("Width")]
		public ISpread<float> FWidth;
	    [Output("Selection Start")]
	    public ISpread<int> FSelStart;
	    [Output("Selection Length")]
	    public ISpread<int> FSelLength;

        [Import()]
        public ILogger FLogger;

		//gui controls
		TextBox FTextBox = new TextBox();
		#endregion fields & pins

		#region constructor and init

		public GUITextBoxNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			//clear controls in case init is called multiple times
			Controls.Clear();
			FTextBox.Dock = DockStyle.Fill;
			FTextBox.BorderStyle = BorderStyle.None;
			FTextBox.Height = 20;
			//FTextBox.Margin = new Padding(2);
			FTextBox.BackColor = Color.FromArgb(255, 220, 220, 220);
			FTextBox.ForeColor = Color.Black;

			//add to controls
			Controls.Add(FTextBox);
		}
		#endregion constructor and init

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FSet[0]) FTextBox.Text = FIn[0];
			FOut[0] = FTextBox.Text;
		    FSelStart[0] = FTextBox.SelectionStart;
		    FSelLength[0] = FTextBox.SelectionLength;
		    if (FSelect[0])
		    {
		        FTextBox.SelectionStart = FSelStartIn[0];
		        FTextBox.SelectionLength = FSelLengthIn[0];
            }
            if (FOut.IsChanged)
			{
				Graphics graphics = Graphics.FromImage(new Bitmap(1, 1));
				SizeF size = graphics.MeasureString(FOut[0], new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel));
				FWidth[0] = size.Width;
			}
			if(FSelectAll[0])
			{
				FTextBox.SelectAll();
			}
		}
	}
}
