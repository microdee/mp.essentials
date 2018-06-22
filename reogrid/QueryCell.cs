using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using unvell.ReoGrid;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.reogrid
{
    public abstract class ReoGridQueryCellNode : IPluginEvaluate
    {
        protected abstract ReferenceRange CreateRange(int i);

        protected virtual bool InputIsChanged()
        {
            return false;
        }

        protected abstract int Spreadmax();

        [Output("Cell Range")]
        public ISpread<ReferenceRange> FOutput;

        [Output("Flat Text")]
        public ISpread<ISpread<string>> FFlatString;
        [Output("Flat Values")]
        public ISpread<ISpread<double>> FFlatVals;
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
                    slc;

                for (int i = 0; i < slc; i++)
                {
                    var currrange = CreateRange(i);
                    FOutput[i] = currrange;
                    FRows[i] = currrange.Rows;
                    FCols[i] = currrange.Cols;

                    FFlatString[i].SliceCount = 0;
                    FFlatVals[i].SliceCount = 0;

                    foreach (var cell in currrange.Cells)
                    {
                        var currtext = cell.Data?.ToString() ?? "";
                        var isval = double.TryParse(currtext, out var currval);
                        var outval = isval ? currval : 0.0;
                        if (currtext.Equals("True", StringComparison.InvariantCultureIgnoreCase))
                            outval = 1.0;
                        FFlatVals[i].Add(outval);
                        FFlatString[i].Add(currtext);
                    }
                }
                FOutput.Stream.IsChanged = true;
                //FFlatDate.Stream.IsChanged = true;
            }
        }
    }
    [PluginInfo(
        Name = "Cell",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleReogridCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FRow;
        [Input("Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FCol;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[FRow[i], FCol[i], 1, 1];
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
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeReoGridCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Top Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FTopRow;
        [Input("Left Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FLeftCol;
        [Input("Row Count", DefaultValue = 1, Order = 102)]
        public IDiffSpread<int> FBottomRow;
        [Input("Column Count", DefaultValue = 1, Order = 103)]
        public IDiffSpread<int> FRightCol;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[FTopRow[i], FLeftCol[i], FBottomRow[i], FRightCol[i]];
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
        Category = "ReoGrid",
        Author = "microdee",
        Version = "VectorRange",
        Tags = "office, document, query"
    )]
    public class VectorRangeReoGridCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Range", DefaultValue = 1, Order = 100, DimensionNames = new [] {"T ", "L ", "Rs ", "Cs "})]
        public IDiffSpread<Vector4D> FRange;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[(int)FRange[i].x, (int)FRange[i].y, (int)FRange[i].z, (int)FRange[i].w];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged || FRange.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FInput, FRange);
        }
    }

    [PluginInfo(
        Name = "Cell",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringReoGridCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Query Text", DefaultString = "A1", Order = 100)]
        public IDiffSpread<string> FQuery;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[FQuery[i]];
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
        Name = "Cell",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Selection",
        Tags = "office, document, query"
    )]
    public class SelectionReoGridCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[FInput[i].SelectionRange];
        }

        protected override bool InputIsChanged()
        {
            return true;
        }

        protected override int Spreadmax()
        {
            return FInput.SliceCount;
        }
    }

    [PluginInfo(
        Name = "Split",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "ReferenceRange",
        Tags = "office, document, query"
    )]
    public class ExcelRangeOoXmlCellQueryNode : ReoGridQueryCellNode
    {
        [Input("Range")]
        public IDiffSpread<ReferenceRange> FInput;

        protected override ReferenceRange CreateRange(int i)
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
