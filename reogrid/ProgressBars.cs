using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid;
using unvell.ReoGrid.CellTypes;
using unvell.ReoGrid.Graphics;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.reogrid
{
    public class SetProgressbarBehavior : WorkbookBehavior
    {
        public RangePosition Range { get; set; }
        public string WorksheetName { get; set; }
        public bool Add { get; set; }
        public bool Remove { get; set; }

        public override void ExternalAction(ReoGridControl grid, SpreadsheetEditorNode node)
        {
            base.ExternalAction(grid, node);

            var worksheet = string.IsNullOrWhiteSpace(WorksheetName)
                ? grid.CurrentWorksheet
                : grid.Worksheets[WorksheetName];

            if (Add)
            {
                worksheet.IterateCells(Range, (r, c, cell) =>
                {
                    worksheet[r, c] = new ProgressCell
                    {
                        BottomColor = new SolidColor(100, 100, 100),
                        TopColor = new SolidColor(80, 80, 80)
                    };
                    return true;
                });
            }

            if (Remove)
            {
                worksheet.IterateCells(Range, (r, c, cell) =>
                {
                    worksheet[r, c] = new CellBody();
                    return true;
                });
            }
        }
    }

    public abstract class ReoGridSetProgressbarNode : IPluginEvaluate
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
        [Input("Add", IsBang = true)]
        public IDiffSpread<bool> FAdd;
        [Input("Remove", IsBang = true)]
        public IDiffSpread<bool> FRemove;
        [Input("Target Worksheet")]
        public IDiffSpread<string> FTarget;

        [Output("Behavior Out")]
        public ISpread<SetProgressbarBehavior> FOutput;

        public void Evaluate(int spreadmax)
        {
            var changed = InputIsChanged() ||
                          FOtherBehavs.IsChanged ||
                          FAdd.IsChanged ||
                          FRemove.IsChanged ||
                          FTarget.IsChanged;
            FOutput.Stream.IsChanged = false;
            //FFlatDate.Stream.IsChanged = false;
            if (changed)
            {
                var slc = Math.Max(SpreadUtils.SpreadMax(FAdd, FRemove, FTarget), Spreadmax());
                FOutput.SliceCount = slc;

                for (int i = 0; i < slc; i++)
                {
                    if (FOutput[i] == null) FOutput[i] = new SetProgressbarBehavior();
                    FOutput[i].Range = CreateRange(i);
                    FOutput[i].Add = FAdd[i];
                    FOutput[i].Remove = FRemove[i];
                    FOutput[i].WorksheetName = FTarget[i];
                }

                if (slc > 0) FOutput[0].Others = FOtherBehavs;
                FOutput.Stream.IsChanged = true;
            }
        }
    }
    [PluginInfo(
        Name = "SetProgressBar",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Single",
        Tags = "office, document, query"
    )]
    public class SingleReogridSetProgressBarNode : ReoGridSetProgressbarNode
    {
        [Input("Row", DefaultValue = 1, Order = 100)]
        public IDiffSpread<int> FRow;
        [Input("Column", DefaultValue = 1, Order = 101)]
        public IDiffSpread<int> FCol;

        protected override RangePosition CreateRange(int i)
        {
            return RangePosition.FromCellPosition(FRow[i], FCol[i], 1, 1);
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
        Name = "SetProgressBar",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Range",
        Tags = "office, document, query"
    )]
    public class RangeReoGridSetProgressBarNode : ReoGridSetProgressbarNode
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
            return RangePosition.FromCellPosition(FTopRow[i], FLeftCol[i], FBottomRow[i], FRightCol[i]);
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
        Name = "SetProgressBar",
        Category = "ReoGrid.Behavior",
        Author = "microdee",
        Version = "Query",
        Tags = "office, document, query"
    )]
    public class StringReoGridSetProgressBarNode : ReoGridSetProgressbarNode
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
