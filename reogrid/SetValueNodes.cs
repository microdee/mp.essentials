using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using unvell.ReoGrid;
using unvell.ReoGrid.Utility;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.reogrid
{
    public class SetCellDataBehavior : WorkbookBehavior
    {
        public RangePosition Range { get; set; }
        public string WorksheetName { get; set; }
        public ISpread<string> FlatText { get; set; }
        public ISpread<double> FlatValue { get; set; }
        public bool IsChanged { get; set; }

        public override void ExternalAction(ReoGridControl grid, SpreadsheetEditorNode node)
        {
            base.ExternalAction(grid, node);
            if (!IsChanged) return;

            var worksheet = string.IsNullOrWhiteSpace(WorksheetName)
                ? grid.CurrentWorksheet
                : grid.Worksheets[WorksheetName];

            int i = 0;
            worksheet.IterateCells(Range, (r, c, cell) =>
            {
                cell.Data = string.IsNullOrWhiteSpace(FlatText[i]) ? (object)FlatValue[i] : (object)FlatText[i];
                i++;
                return true;
            });
            IsChanged = false;
        }
    }

    public abstract class ReoGridSetDataNode : IPluginEvaluate
    {
        protected abstract RangePosition CreateRange(int i);

        protected virtual bool InputIsChanged()
        {
            return false;
        }

        protected virtual int Spreadmax()
        {
            return 0;
        }

        [Input("Behavior In")]
        public IDiffSpread<WorkbookBehavior> FOtherBehavs;
        [Input("Flat Text")]
        public IDiffSpread<ISpread<string>> FFlatString;
        [Input("Flat Values")]
        public IDiffSpread<ISpread<double>> FFlatVals;
        [Input("Target Worksheet")]
        public IDiffSpread<string> FTarget;

        [Input("Set On Change", Visibility = PinVisibility.Hidden, DefaultBoolean = true)]
        public IDiffSpread<bool> FAutoSet;

        [Output("Behavior Out")]
        public ISpread<SetCellDataBehavior> FOutput;

        public void Evaluate(int spreadmax)
        {
            var changed = InputIsChanged() ||
                          FOtherBehavs.IsChanged ||
                          FFlatString.IsChanged ||
                          FFlatVals.IsChanged ||
                          FAutoSet.IsChanged ||
                          FTarget.IsChanged;
            FOutput.Stream.IsChanged = false;
            //FFlatDate.Stream.IsChanged = false;
            if (changed)
            {
                var slc = Math.Max(SpreadUtils.SpreadMax(FFlatString, FFlatVals, FTarget), Spreadmax());
                FOutput.SliceCount = slc;

                for (int i = 0; i < slc; i++)
                {
                    if(FOutput[i] == null || FOutput[i] == FOutput[i-1]) FOutput[i] = new SetCellDataBehavior();
                    FOutput[i].Range = CreateRange(i);
                    FOutput[i].FlatText = FFlatString[i];
                    FOutput[i].FlatValue = FFlatVals[i];
                    FOutput[i].WorksheetName = FTarget[i];
                    FOutput[i].IsChanged = true;
                }

                if (slc > 0) FOutput[0].Others = FOtherBehavs;
                if(FAutoSet[0])
                    FOutput.Stream.IsChanged = true;
            }
        }
    }
    [PluginInfo(
        Name = "SetData",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleReogridSetDataNode : ReoGridSetDataNode
    {
        [Input("Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FRow;
        [Input("Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FCol;

        protected override RangePosition CreateRange(int i)
        {
            return RangePosition.FromCellPosition(FRow[i], FCol[i], FRow[i], FCol[i]);
        }

        protected override bool InputIsChanged()
        {
            return FRow.IsChanged || FCol.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FCol, FRow);
        }
    }

    [PluginInfo(
        Name = "SetData",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeReoGridSetDataNode : ReoGridSetDataNode
    {
        [Input("Top Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FTopRow;
        [Input("Left Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FLeftCol;
        [Input("Row Count", DefaultValue = 1, Order = 102)]
        public IDiffSpread<int> FBottomRow;
        [Input("Column Count", DefaultValue = 1, Order = 103)]
        public IDiffSpread<int> FRightCol;

        protected override RangePosition CreateRange(int i)
        {
            return RangePosition.FromCellPosition(FTopRow[i], FLeftCol[i], FTopRow[i] + FBottomRow[i] - 1, FLeftCol[i] + FRightCol[i] - 1);
        }

        protected override bool InputIsChanged()
        {
            return FTopRow.IsChanged || FLeftCol.IsChanged || FBottomRow.IsChanged || FRightCol.IsChanged;
        }

        protected override int Spreadmax()
        {
            return SpreadUtils.SpreadMax(FTopRow, FLeftCol, FBottomRow, FRightCol);
        }
    }

    [PluginInfo(
        Name = "SetData",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringReoGridSetDataNode : ReoGridSetDataNode
    {
        [Input("Range Address", DefaultString = "A1", Order = 100)]
        public IDiffSpread<string> FQuery;

        protected override RangePosition CreateRange(int i)
        {
            return new RangePosition(FQuery[i]);
        }

        protected override bool InputIsChanged()
        {
            return FQuery.IsChanged;
        }

        protected override int Spreadmax()
        {
            return FQuery.SliceCount;
        }
    }
}
