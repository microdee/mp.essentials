using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using unvell.ReoGrid;
using unvell.ReoGrid.Actions;
using unvell.ReoGrid.Graphics;

namespace mp.essentials.reogrid
{
    public delegate void ColorPickerClosedEventHandler(ColorPickerWrap wrap);

    public class ColorPickerWrap
    {
        public ColorPickerDialog Dialog { get; } = new ColorPickerDialog();

        public RangePosition Range;
        public ReferenceRange Cells;
        public SpreadsheetEditorNode Host;
        private WorksheetRangeStyle _prevStyle;

        public ColorPickerWrap((ReferenceRange cells, RangePosition range) selected, SpreadsheetEditorNode host)
        {
            (Cells, Range) = selected;
            Host = host;

            SpreadsheetEditorNode.GetCursorPos(out var curpoint);

            _prevStyle = new WorksheetRangeStyle(Cells.Style);

            Dialog.Color = Cells.Style.BackColor;
            Dialog.Text = "Pick color for " + Range.ToAddress();
            Dialog.PreviewColorChanged += (sender, args) =>
            {
                //foreach (var cell in Cells.Cells)
                //    cell.Style.BackColor = Dialog.Color;
                var style = Cells.Style;
                style.BackColor = Dialog.Color;
                Host.Grid.CurrentWorksheet.RequestInvalidate();
                //Host.Grid.DoAction(Cells.Worksheet, new SetRangeStyleAction(Range, style));

                Host.IsChanged = true;
            };
            Dialog.Closed += (sender, args) =>
            {
                Host.Grid.DoAction(Cells.Worksheet, new SetRangeStyleAction(Range, Cells.Style));
                Closed?.Invoke(this);
            };
            Dialog.Show();
            Dialog.StartPosition = FormStartPosition.Manual;
            Dialog.SetDesktopLocation(curpoint.X, curpoint.Y);
        }

        public event ColorPickerClosedEventHandler Closed;
    }
}
