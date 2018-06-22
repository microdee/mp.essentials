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
            this.grid = new unvell.ReoGrid.ReoGridControl();
            this.CellValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.BackColor = System.Drawing.Color.White;
            this.grid.ColumnHeaderContextMenuStrip = null;
            this.grid.LeadHeaderContextMenuStrip = null;
            this.grid.Location = new System.Drawing.Point(0, 16);
            this.grid.Margin = new System.Windows.Forms.Padding(0);
            this.grid.Name = "grid";
            this.grid.RowHeaderContextMenuStrip = null;
            this.grid.Script = null;
            this.grid.SheetTabContextMenuStrip = null;
            this.grid.SheetTabNewButtonVisible = true;
            this.grid.SheetTabVisible = true;
            this.grid.SheetTabWidth = 60;
            this.grid.ShowScrollEndSpacing = true;
            this.grid.Size = new System.Drawing.Size(400, 284);
            this.grid.TabIndex = 0;
            this.grid.Text = "reoGridControl1";
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
            this.Controls.Add(this.grid);
            this.Name = "SpreadsheetEditorNode";
            this.Size = new System.Drawing.Size(400, 300);
            this.Resize += new System.EventHandler(this.OnResize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private unvell.ReoGrid.ReoGridControl grid;
        private System.Windows.Forms.TextBox CellValue;
    }
}
