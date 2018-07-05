namespace mp.essentials.reogrid
{
    partial class SpreadsheetEditorNode
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Grid = new unvell.ReoGrid.ReoGridControl();
            this.CellValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Grid
            // 
            this.Grid.BackColor = System.Drawing.Color.White;
            this.Grid.ColumnHeaderContextMenuStrip = null;
            this.Grid.LeadHeaderContextMenuStrip = null;
            this.Grid.Location = new System.Drawing.Point(0, 16);
            this.Grid.Margin = new System.Windows.Forms.Padding(0);
            this.Grid.Name = "Grid";
            this.Grid.RowHeaderContextMenuStrip = null;
            this.Grid.Script = null;
            this.Grid.SheetTabContextMenuStrip = null;
            this.Grid.SheetTabNewButtonVisible = true;
            this.Grid.SheetTabVisible = true;
            this.Grid.SheetTabWidth = 60;
            this.Grid.ShowScrollEndSpacing = true;
            this.Grid.Size = new System.Drawing.Size(400, 284);
            this.Grid.TabIndex = 0;
            this.Grid.Text = "reoGridControl1";
            // 
            // CellValue
            // 
            this.CellValue.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.CellValue.Dock = System.Windows.Forms.DockStyle.Top;
            this.CellValue.Location = new System.Drawing.Point(0, 0);
            this.CellValue.Name = "CellValue";
            this.CellValue.Size = new System.Drawing.Size(400, 13);
            this.CellValue.TabIndex = 1;
            this.CellValue.Enter += new System.EventHandler(this.OnCellValueFocus);
            this.CellValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnCellValueKeyDown);
            // 
            // SpreadsheetEditorNode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CellValue);
            this.Controls.Add(this.Grid);
            this.Name = "SpreadsheetEditorNode";
            this.Size = new System.Drawing.Size(400, 300);
            this.Resize += new System.EventHandler(this.OnResize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public unvell.ReoGrid.ReoGridControl Grid;
        private System.Windows.Forms.TextBox CellValue;
    }
}
