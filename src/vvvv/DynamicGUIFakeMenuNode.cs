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
	[PluginInfo(Name = "FakeMenu", Category = "GUI", Version = "Dynamic", Help = "Template with a dynamic set of gui elements", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class DynamicGUIFakeMenuNode : UserControl, IPluginEvaluate
	{
		#region fields & pins
		[Input("Button Names", DefaultString = "Bang")]
		IDiffSpread<string> FButtonNames;
		[Input("Highlight")]
		IDiffSpread<bool> FHighlight;
		[Input("Disable")]
		IDiffSpread<bool> FDisable;

		[Output("Buttons")]
		ISpread<bool> FButtonOut;

		[Import()]
		ILogger FLogger;

		//layout panels
		TableLayoutPanel FMainPanel = new TableLayoutPanel();
		Dictionary<string, TableLayoutPanel> FTabPanels = new Dictionary<string, TableLayoutPanel>();

		//gui controls lists
		bool[] FButtonClick = new bool[1];

		#endregion fields & pins

		#region constructor and init

		public DynamicGUIFakeMenuNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			this.BackColor = Color.FromArgb(255, 220, 220, 220);
			//create tab panels for the UI controls
			FTabPanels["buttons"] = new TableLayoutPanel();
			FMainPanel.BackColor = Color.FromArgb(255, 220, 220, 220);

			//config table panel 1x1
			FMainPanel.ColumnCount = 1;
			FMainPanel.RowCount = 1;
			FMainPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

			//define size of the columns
			FMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			//table panel fills the whole window
			FMainPanel.Dock = DockStyle.Fill;
			this.Padding = new Padding(0);

			//config table panels and add them to the main table panel
			int i = 0;
			foreach (var tabPanel in FTabPanels.Values) {
				tabPanel.Dock = DockStyle.Fill;
				FMainPanel.Controls.Add(tabPanel, i++, 0);
			}

			//add the main table panel to the window
			Controls.Add(FMainPanel);
		}

		#endregion constructor and init

		#region dynamic control layout

		//buttons
		void LayoutButtons()
		{
			var c = FTabPanels["buttons"].Controls;
			c.Clear();
			FButtonClick = new bool[FButtonNames.SliceCount];

			int i = 0;
			foreach (var name in FButtonNames) {
				var button = new Button();
				button.Text = name;
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderSize = 0;
				button.TabStop = false;
				button.Dock = DockStyle.Top;
				button.Height = 20;
				button.Margin = new Padding(0);
				button.Padding = new Padding(0);
				button.TextAlign = ContentAlignment.MiddleLeft;
				//listen to click event
				button.Click += ButtonClicked;
				//tag it with its index
				button.Tag = i++;
	 			if(FHighlight[(int)button.Tag]) button.BackColor = Color.LightGreen;
				if(FDisable[(int)button.Tag]) button.Enabled = false;
				else button.Enabled = true;
				c.Add(button);
			}
		}

		//called if a button is clicked
		void ButtonClicked(object sender, EventArgs e)
		{
			if (sender is Button) {
				var b = sender as Button;

				//set button click to true to read it in Evaluate()
				FButtonClick[(int)b.Tag] = true;
			}
		}
		#endregion dynamic control layout

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//button setup
			if (FButtonNames.IsChanged || FHighlight.IsChanged || FDisable.IsChanged) {
				LayoutButtons();
				FButtonOut.SliceCount = FButtonNames.SliceCount;
			}

			//set outputs
			for (int i = 0; i < FButtonOut.SliceCount; i++) {
				FButtonOut[i] = FButtonClick[i];
				FButtonClick[i] = false;
			}
		}
	}
}
