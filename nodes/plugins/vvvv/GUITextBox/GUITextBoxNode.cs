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
	[PluginInfo(Name = "TextBox", Category = "GUI", Help = "Template with some gui elements", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class GUITextBoxNode : UserControl, IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", IsSingle = true)]
		ISpread<string> FIn;
		[Input("Set", IsSingle = true)]
		ISpread<bool> FSet;
		[Input("Select All", IsSingle = true, IsBang = true)]
		ISpread<bool> FSelectAll;

		[Output("Output")]
		ISpread<string> FOut;
		[Output("Width")]
		ISpread<float> FWidth;

		[Import()]
		ILogger FLogger;

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
			if(FOut.IsChanged)
			{
				System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(new Bitmap(1, 1));
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
