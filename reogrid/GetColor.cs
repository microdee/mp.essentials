using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using unvell.ReoGrid;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

namespace mp.essentials.reogrid
{
    public abstract class ReoGridGetColorCellNode : IPluginEvaluate
    {
        protected abstract ReferenceRange CreateRange(int i);

        protected virtual bool InputIsChanged()
        {
            return false;
        }

        protected abstract int Spreadmax();

        [Output("Cell Range")]
        public ISpread<ReferenceRange> FOutput;

        [Output("Flat Colors")]
        public ISpread<ISpread<RGBAColor>> FFlatColors;
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
                    FFlatColors.SliceCount =
                    FRows.SliceCount =
                    FCols.SliceCount =
                    slc;

                for (int i = 0; i < slc; i++)
                {
                    var currrange = CreateRange(i);
                    FOutput[i] = currrange;
                    FRows[i] = currrange.Rows;
                    FCols[i] = currrange.Cols;

                    FFlatColors[i].SliceCount = 0;

                    foreach (var cell in currrange.Cells)
                    {
                        var currcol = cell.Style.BackColor;
                        FFlatColors[i].Add(new RGBAColor(
                            currcol.R / 255.0,
                            currcol.G / 255.0,
                            currcol.B / 255.0,
                            currcol.A / 255.0
                        ));
                    }
                }
                FOutput.Stream.IsChanged = true;
                //FFlatDate.Stream.IsChanged = true;
            }
        }
    }
    [PluginInfo(
        Name = "GetColor",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleReoGridGetColorCellNode : ReoGridGetColorCellNode
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
        Name = "GetColor",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeReoGridGetColorCellNode : ReoGridGetColorCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Top Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FTopRow;
        [Input("Left Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FLeftCol;
        [Input("Row Count", DefaultValue = 1, Order = 102)]
        public IDiffSpread<int> FRowCount;
        [Input("Column Count", DefaultValue = 1, Order = 103)]
        public IDiffSpread<int> FColCount;

        protected override ReferenceRange CreateRange(int i)
        {
            return FInput[i].Ranges[FTopRow[i], FLeftCol[i], FRowCount[i], FColCount[i]];
        }

        protected override bool InputIsChanged()
        {
            return FInput.IsChanged || FTopRow.IsChanged || FLeftCol.IsChanged || FRowCount.IsChanged || FColCount.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FInput, FTopRow, FLeftCol, FRowCount, FColCount);
        }
    }
    [PluginInfo(
        Name = "GetColor",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "VectorRange",
        Tags = "office, document, query"
    )]
    public class VectorRangeReoGridGetColorCellNode : ReoGridGetColorCellNode
    {
        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Input("Range", DefaultValue = 1, Order = 100, DimensionNames = new[] { "T ", "L ", "Rs ", "Cs " })]
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
        Name = "GetColor",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringReoGridGetColorCellNode : ReoGridGetColorCellNode
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
        Name = "GetColor",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Selection",
        Tags = "office, document, query"
    )]
    public class SelectionReoGridGetColorCellNode : ReoGridGetColorCellNode
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
}
