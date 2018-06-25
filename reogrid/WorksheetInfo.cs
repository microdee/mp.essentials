using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using static System.Math;

namespace mp.essentials.reogrid
{
    [PluginInfo(
        Name = "WorksheetMetrics",
        Category = "ReoGrid",
        Author = "microdee"
    )]
    public class WorksheetMetricsNode : IPluginEvaluate
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;
        [Input("Active Trimming", DefaultBoolean = true, Visibility = PinVisibility.Hidden)]
        public IDiffSpread<bool> FTrim;

        [Output("Used Range", DimensionNames = new[] { "T ", "L ", "Rs ", "Cs " })]
        public ISpread<Vector4D> FUsedRange;

        [Output("Max Content Rows")]
        public ISpread<int> FMaxContRow;
        [Output("Max Content Columns")]
        public ISpread<int> FMaxContCol;

        [Output("Max Content Rows High")]
        public ISpread<int> FMaxContRowHi;
        [Output("Max Content Columns High")]
        public ISpread<int> FMaxContColHi;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsChanged || FTrim.IsChanged)
            {
                FUsedRange.SliceCount =
                FMaxContCol.SliceCount =
                FMaxContColHi.SliceCount =
                FMaxContRow.SliceCount =
                FMaxContRowHi.SliceCount =
                    FInput.SliceCount;

                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if(FInput[i] == null) continue;
                    var usedrange = FInput[i].UsedRange;

                    if (FTrim[0])
                    {
                        int usedrow = 0, usedcol = 0;

                        for (int r = Min(usedrange.Rows, FInput[i].RowCount - 1); r >= 0; r--)
                        {
                            var found = false;
                            for (int c = Min(usedrange.Cols, FInput[i].ColumnCount - 1); c >= 0; c--)
                            {
                                var cell = FInput[i].Cells[r, c];
                                if (string.IsNullOrWhiteSpace(cell.Data?.ToString())) continue;
                                usedrow = r;
                                found = true;
                                break;
                            }
                            if (found) break;
                        }
                        for (int c = Min(usedrange.Cols, FInput[i].ColumnCount - 1); c >= 0; c--)
                        {
                            var found = false;
                            for (int r = Min(usedrange.Rows, FInput[i].RowCount - 1); r >= 0; r--)
                            {
                                var cell = FInput[i].Cells[r, c];
                                if (string.IsNullOrWhiteSpace(cell.Data?.ToString())) continue;
                                usedcol = c;
                                found = true;
                                break;
                            }
                            if (found) break;
                        }
                        FUsedRange[i] = new Vector4D(0, 0, usedrow, usedcol);
                        FMaxContRow[i] = usedrow + 1;
                        FMaxContCol[i] = usedcol + 1;
                    }
                    else
                    {
                        FUsedRange[i] = new Vector4D(usedrange.Row, usedrange.Col, usedrange.Rows, usedrange.Cols);
                        FMaxContRow[i] = FInput[i].MaxContentRow + 1;
                        FMaxContCol[i] = FInput[i].MaxContentCol + 1;
                    }
                    FMaxContRowHi[i] = FMaxContRow[i] - 1;
                    FMaxContColHi[i] = FMaxContCol[i] - 1;
                }
            }
        }
    }
}
