using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using unvell.ReoGrid;
using unvell.ReoGrid.CellTypes;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.reogrid
{
    public abstract class ReoGridGetButtonNode : IPluginEvaluate
    {
        protected abstract ReferenceRange CreateRange(int i);

        protected virtual bool InputIsChanged()
        {
            return false;
        }

        protected abstract int Spreadmax();

        [Input("Worksheet")]
        public IDiffSpread<Worksheet> FInput;

        [Output("Cell Range")]
        public ISpread<ReferenceRange> FOutput;

        [Output("Output", IsBang = true)]
        public ISpread<ISpread<bool>> FFlatOut;
        //[Output("Flat Dates")]
        //public ISpread<ISpread<DateTime>> FFlatDate;

        private Dictionary<Worksheet, Dictionary<Cell, int>> _buttonbangs = new Dictionary<Worksheet, Dictionary<Cell, int>>();

        public void Evaluate(int spreadmax)
        {
            var changed = InputIsChanged();
            FOutput.Stream.IsChanged = false;
            //FFlatDate.Stream.IsChanged = false;
            var slc = Spreadmax();
            FOutput.SliceCount =
                FFlatOut.SliceCount =
                    slc;

            if (changed)
            {

                for (int i = 0; i < slc; i++)
                {
                    if (!_buttonbangs.ContainsKey(FInput[i]))
                    {
                        _buttonbangs.Add(FInput[i], new Dictionary<Cell, int>());
                    }

                    var buttondict = _buttonbangs[FInput[i]];
                    var currrange = CreateRange(i);
                    FOutput[i] = currrange;

                    foreach (var cell in currrange.Cells)
                    {
                        if (!buttondict.ContainsKey(cell))
                        {
                            buttondict.Add(cell, 0);
                        }
                        if (cell.Body is ButtonCell bcell)
                        {
                            bcell.Click -= OnButtonCellClick;
                            bcell.Click += OnButtonCellClick;
                        }
                    }
                }
                FOutput.Stream.IsChanged = true;
                //FFlatDate.Stream.IsChanged = true;
            }

            for (int i = 0; i < slc; i++)
            {
                if (!_buttonbangs.ContainsKey(FInput[i])) continue;

                var buttondict = _buttonbangs[FInput[i]];
                var currrange = FOutput[i];

                FFlatOut[i].SliceCount = buttondict.Count;

                if(currrange == null) continue;

                int ii = 0;
                foreach (var cell in currrange.Cells)
                {
                    if(!buttondict.ContainsKey(cell)) continue;
                    FFlatOut[i][ii] = buttondict[cell] > 0;
                    buttondict[cell] = 0;
                    ii++;
                }
            }
        }

        private void OnButtonCellClick(object sender, EventArgs eventArgs)
        {
            if (sender is ButtonCell bcell)
            {
                var cell = bcell.Cell;
                if (_buttonbangs.ContainsKey(cell.Worksheet))
                {
                    var buttondict = _buttonbangs[cell.Worksheet];
                    if (buttondict.ContainsKey(cell))
                    {
                        buttondict[cell]++;
                    }
                    else
                    {
                        buttondict.Add(cell, 1);
                    }
                }
                else
                {
                    _buttonbangs.Add(cell.Worksheet, new Dictionary<Cell, int>());
                    _buttonbangs[cell.Worksheet].Add(cell, 1);
                }
            }
        }
    }
    [PluginInfo(
        Name = "GetButton",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleReogridGetButtonNode : ReoGridGetButtonNode
    {

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
        Name = "GetButton",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeReoGridGetButtonNode : ReoGridGetButtonNode
    {
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
        Name = "GetButton",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "VectorRange",
        Tags = "office, document, query"
    )]
    public class VectorRangeReoGridGetButtonNode : ReoGridGetButtonNode
    {
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
        Name = "GetButton",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringReoGridGetButtonNode : ReoGridGetButtonNode
    {
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
        Name = "GetButton",
        Category = "ReoGrid",
        Author = "microdee",
        Version = "Selection",
        Tags = "office, document, query"
    )]
    public class SelectionReoGridGetButtonNode : ReoGridGetButtonNode
    {
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
