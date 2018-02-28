using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using md.stdl.Coding;
using OfficeOpenXml;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Excel
{
    public enum CellType
    {
        Text,
        Number,
        Other
    }
    public abstract class AbstractOoXmlCellQueryNode : IPluginEvaluate
    {
        protected abstract ExcelRangeBase CreateRange(int i);

        protected virtual bool InputIsChanged()
        {
            return false;
        }

        protected abstract int Spreadmax();

        [Output("Cell Range")]
        public ISpread<ExcelRangeBase> FOutput;
        
        [Output("Flat Text")]
        public ISpread<ISpread<string>> FFlatString;
        [Output("Flat Values")]
        public ISpread<ISpread<double>> FFlatVals;
        [Output("Type")]
        public ISpread<ISpread<CellType>> FType;
        [Output("Rows")]
        public ISpread<int> FRows;
        [Output("Columns")]
        public ISpread<int> FCols;
        //[Output("Flat Dates")]
        //public ISpread<ISpread<DateTime>> FFlatDate;

        public void Evaluate(int spreadmax)
        {
            var changed = InputIsChanged();
            FOutput.Stream.IsChanged = false;
            //FFlatDate.Stream.IsChanged = false;
            if (changed)
            {
                var slc = Spreadmax();
                FOutput.SliceCount =
                    FFlatVals.SliceCount =
                    FFlatString.SliceCount =
                    FRows.SliceCount =
                    FCols.SliceCount =
                    FType.SliceCount =
                    slc;

                for (int i = 0; i < slc; i++)
                {
                    var currrange = CreateRange(i);
                    FOutput[i] = currrange;
                    FRows[i] = currrange.Rows;
                    FCols[i] = currrange.Columns;
                    if (currrange.Value is object[,] array)
                    {
                        FFlatString[i].SliceCount = 
                            FFlatVals[i].SliceCount = 
                            FType[i].SliceCount = currrange.Rows * currrange.Columns;

                        int jj = 0;
                        for (int j = 0; j < currrange.Rows; j++)
                        {
                            for (int k = 0; k < currrange.Columns; k++)
                            {
                                try
                                {
                                    FFlatString[i][jj] = array[j, k].ToString();
                                    if (array[j, k].IsNumeric())
                                    {
                                        FFlatVals[i][jj] = (double)array[j, k];
                                        FType[i][jj] = CellType.Number;
                                    }
                                    else
                                    {
                                        FFlatVals[i][jj] = 0;
                                        FType[i][jj] = array[j, k] is string ? CellType.Text : CellType.Other;
                                    }
                                }
                                catch { }

                                jj++;
                            }
                        }
                    }
                    else if (currrange.Value.IsNumeric())
                    {
                        FFlatString[i].SliceCount = 1;
                        FFlatVals[i].SliceCount = 1;
                        FType[i].SliceCount = 1;

                        FFlatString[i][0] = currrange.Value.ToString();
                        FFlatVals[i][0] = (double)currrange.Value;
                        FType[i][0] = CellType.Number;
                    }
                    else
                    {
                        FFlatString[i].SliceCount = 1;
                        FFlatVals[i].SliceCount = 1;
                        FType[i].SliceCount = 1;

                        FFlatString[i][0] = currrange.Value.ToString();
                        FFlatVals[i][0] = 0;
                        FType[i][0] = currrange.Value is string ? CellType.Text : CellType.Other;
                    }
                }
                FOutput.Stream.IsChanged = true;
                //FFlatDate.Stream.IsChanged = true;
            }
        }
    }

    [PluginInfo(
        Name = "Cell",
        Category = "OoXml",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleOoXmlCellQueryNode : AbstractOoXmlCellQueryNode
    {
        [Input("Worksheet")]
        public IDiffSpread<ExcelWorksheet> FInput;

        [Input("Row", Order = 100)]
        public IDiffSpread<int> FRow;
        [Input("Column", Order = 101)]
        public IDiffSpread<int> FCol;

        protected override ExcelRangeBase CreateRange(int i)
        {
            return FInput[i].Cells[FRow[i] + 1, FCol[i] + 1];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged || FRow.IsChanged || FCol.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FInput, FCol, FRow);
        }
    }

    [PluginInfo(
        Name = "Cell",
        Category = "OoXml",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeOoXmlCellQueryNode : AbstractOoXmlCellQueryNode
    {
        [Input("Worksheet")]
        public IDiffSpread<ExcelWorksheet> FInput;

        [Input("Top Row", Order = 100)]
        public IDiffSpread<int> FTopRow;
        [Input("Left Column", Order = 101)]
        public IDiffSpread<int> FLeftCol;
        [Input("Bottom Row", DefaultValue = 1, Order = 102)]
        public IDiffSpread<int> FBottomRow;
        [Input("Right Column", DefaultValue = 1, Order = 103)]
        public IDiffSpread<int> FRightCol;

        protected override ExcelRangeBase CreateRange(int i)
        {
            return FInput[i].Cells[FTopRow[i] + 1, FLeftCol[i] + 1, FBottomRow[i] + 1, FRightCol[i] + 1];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged || FTopRow.IsChanged || FLeftCol.IsChanged || FBottomRow.IsChanged || FRightCol.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FInput, FTopRow, FLeftCol, FBottomRow, FRightCol);
        }
    }

    [PluginInfo(
        Name = "Cell",
        Category = "OoXml",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringOoXmlCellQueryNode : AbstractOoXmlCellQueryNode
    {
        [Input("Worksheet")]
        public IDiffSpread<ExcelWorksheet> FInput;

        [Input("Query Text", DefaultString = "A1", Order = 100)]
        public IDiffSpread<string> FQuery;

        protected override ExcelRangeBase CreateRange(int i)
        {
            return FInput[i].Cells[FQuery[i]];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged || FQuery.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FInput, FQuery);
        }
    }

    [PluginInfo(
        Name = "Split",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelRange",
        Tags = "office, document, query"
    )]
    public class ExcelRangeOoXmlCellQueryNode : AbstractOoXmlCellQueryNode
    {
        [Input("Range")]
        public IDiffSpread<ExcelRangeBase> FInput;

        protected override ExcelRangeBase CreateRange(int i)
        {
            return FInput[i];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged;
        }

        protected override int Spreadmax()
        {
            return FInput.SliceCount;
        }
    }
}
