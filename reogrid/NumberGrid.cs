using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using unvell.ReoGrid;
using unvell.ReoGrid.Actions;
using unvell.ReoGrid.CellTypes;
using unvell.ReoGrid.DataFormat;
using unvell.ReoGrid.Events;
using unvell.ReoGrid.Graphics;
using unvell.ReoGrid.Interaction;
using unvell.ReoGrid.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using WPoint = System.Drawing.Point;

namespace mp.essentials.reogrid
{
    public abstract class WorkbookBehavior
    {
        public ISpread<WorkbookBehavior> Others { get; set; }

        public virtual void ExternalAction(ReoGridControl grid, SpreadsheetEditorNode node)
        {
            if (Others != null)
            {
                foreach (var behavior in Others)
                {
                    behavior?.ExternalAction(grid, node);
                }
            }
        }
    }

    public class WorksheetExtraData
    {
        public readonly Worksheet Worksheet;
        public XElement Xml;
        public readonly Dictionary<RangePosition, RadioButtonGroup> RadioGroups = new Dictionary<RangePosition, RadioButtonGroup>();
        public readonly Dictionary<(int r, int c), string> EnumCells = new Dictionary<(int r, int c), string>();

        public WorksheetExtraData(Worksheet ws)
        {
            Worksheet = ws;
        }
    }

    [PluginInfo(
        Name = "Spreadsheet",
        Category = "ReoGrid",
        Author = "microdee",
        AutoEvaluate = true,
        InitialBoxWidth = 400,
        InitialBoxHeight = 300,
        InitialComponentMode = TComponentMode.InAWindow
    )]
    public partial class SpreadsheetEditorNode : UserControl, IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        public IHDEHost Hde;
        
        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool GetCursorPos(out WPoint lpPoint);

        [Config("Workbook Data")]
        public IDiffSpread<string> FWorkbookData;
        [Config("Drag Speed Normal", DefaultValue = 0.01)]
        public IDiffSpread<double> FNormDragSpeed;
        [Config("Drag Speed Slow", DefaultValue = 0.001)]
        public IDiffSpread<double> FSlowDragSpeed;
        [Config("Drag Speed Fast", DefaultValue = 1.0)]
        public IDiffSpread<double> FFastDragSpeed;
        [Config("Dots per Step", DefaultValue = 5)]
        public IDiffSpread<int> FDotsPerStep;

        [Input("Behaviors")]
        public ISpread<WorkbookBehavior> FWorkbookBehaviors;

        [Output("Worksheet")]
        public ISpread<Worksheet> FWorksheet;
        [Output("Worksheet Name")]
        public ISpread<string> FNames;
        [Output("Worksheet Xml")]
        public ISpread<XElement> FXml;
        //[Output("Keycode")]
        //public ISpread<uint> FKc;

        private bool _initLoaded = false;
        private bool _loading = false;
        private bool _dragging = false;
        private bool _dragSlow = false;
        private bool _dragFast = false;
        private bool _dragChangedVal = false;
        private bool _isChanged = false;
        private bool _isEnteringNumber = false;
        private bool _isEnteringText = false;
        private float _prevMouseY = -1;
        private float _accMouseY = 0;
        private Worksheet _prevWorksheet;

        private WPoint _dragInitCursorPos;
        private float _dragInitY;
        private readonly Dictionary<Worksheet, WorksheetExtraData> _wsxdata = new Dictionary<Worksheet, WorksheetExtraData>();
        private readonly List<string> _enums = new List<string>();

        public ContextMenuStrip CellCtxMenu;
        public ToolStripMenuItem CellDropDownEnumSelector;

        private void AddWorksheetData(Worksheet ws, Action<WorksheetExtraData> addingAction)
        {
            if (_wsxdata.ContainsKey(ws))
            {
                addingAction(_wsxdata[ws]);
            }
            else
            {
                var wsxd = new WorksheetExtraData(ws);
                _wsxdata.Add(ws, wsxd);
                addingAction(wsxd);
            }
        }

        private void OnCellFunctionMenuClick(object o, EventArgs eventArgs)
        {
            var selected = GetSelectedCells();
            if (o is ToolStripMenuItem mi)
            {
                switch (mi.Text)
                {
                    case "Merge":
                    {
                        grid.DoAction(new MergeRangeAction(selected.range));
                        break;
                    }
                    case "Unmerge":
                    {
                        grid.DoAction(new UnmergeRangeAction(selected.range));
                        break;
                    }
                    case "Freeze Top":
                    {
                        grid.CurrentWorksheet.FreezeToCell(selected.range.EndRow + 1, selected.range.EndCol + 1, FreezeArea.Top);
                        SaveChanges();
                        break;
                    }
                    case "Freeze Left":
                    {
                        grid.CurrentWorksheet.FreezeToCell(selected.range.EndRow + 1, selected.range.EndCol + 1, FreezeArea.Left);
                        SaveChanges();
                        break;
                    }
                    case "Freeze Top-left":
                    {
                        grid.CurrentWorksheet.FreezeToCell(selected.range.EndRow + 1, selected.range.EndCol + 1, FreezeArea.LeftTop);
                        SaveChanges();
                         break;
                    }
                    case "Unfreeze":
                    {
                        grid.CurrentWorksheet.Unfreeze();
                        SaveChanges();
                        break;
                    }
                }
                _isChanged = true;
            }
        }

        private void OnCellTypeMenuClick(object o, EventArgs args)
        {
            var selected = GetSelectedCells();
            if (o is ToolStripMenuItem mi)
            {
                switch (mi.Text)
                {
                    case "Plain":
                    {
                        selected.cells.Cells.ForEach((cell, i) => cell.Body = null);
                        break;
                    }
                    case "Checkbox":
                    {
                        selected.cells.Cells.ForEach((cell, i) =>
                        {
                            var button = new CheckBoxCell();
                            button.Click += (sender, eventArgs) => SaveChanges();
                            cell.Body = button;
                        });
                        break;
                    }
                    case "Radio Button":
                    {
                        var radiogroup = new RadioButtonGroup();

                        AddWorksheetData(grid.CurrentWorksheet, data =>
                        {
                            if (data.RadioGroups.ContainsKey(selected.range))
                                data.RadioGroups[selected.range] = radiogroup;
                            else data.RadioGroups.Add(selected.range, radiogroup);
                        });

                        selected.cells.Cells.ForEach((cell, i) =>
                        {
                            var button = new RadioButtonCell { RadioGroup = radiogroup };
                            button.Click += (sender, eventArgs) => SaveChanges();
                            cell.Body = button;
                        });
                        break;
                    }
                    case "Horizontal Bar":
                    {
                        selected.cells.Cells.ForEach((cell, i) => cell.Body = new HorizontalProgressCell());
                        break;
                    }
                    case "Vertical Bar":
                    {
                        selected.cells.Cells.ForEach((cell, i) => cell.Body = new VerticalProgressCell());
                        break;
                    }
                    case "Button":
                    {
                        selected.cells.Cells.ForEach((cell, i) => cell.Body = new ButtonCell());
                        break;
                    }
                }
                _isChanged = true;
                SaveChanges();
            }
        }

        private void OnDropdownCellTypeSelection(object o, EventArgs args)
        {
            if (o is ToolStripMenuItem mi)
            {
                var selected = GetSelectedCells();
                var entries = GetEnumEntries(mi.Text);
                selected.cells.Cells.ForEach((cell, i) =>
                {
                    var addrtuple = (cell.Row, cell.Column);
                    AddWorksheetData(grid.CurrentWorksheet, data =>
                    {
                        if (data.EnumCells.ContainsKey(addrtuple)) data.EnumCells[addrtuple] = mi.Text;
                        else data.EnumCells.Add(addrtuple, mi.Text);
                    });

                    var dropdown = new DropdownListCell(entries);
                    dropdown.SelectedItemChanged += (sender, eventArgs) => SaveChanges();
                    cell.Body = dropdown;
                });

                _isChanged = true;
                SaveChanges();
            }
        }

        [ImportingConstructor]
        public SpreadsheetEditorNode()
        {
            InitializeComponent();
            grid.ControlStyle = Styles.VvvvStyle();

            grid.CellsSelectionCursor = Cursors.Cross;
            grid.EntireSheetSelectionCursor = Cursors.Cross;
            grid.FullColumnSelectionCursor = Cursors.UpArrow;
            grid.FullRowSelectionCursor = Cursors.UpArrow;

            CellCtxMenu = new ContextMenuStrip();
            var enumTypeFilter = new ToolStripTextBox("Filter");
            CellDropDownEnumSelector = new ToolStripMenuItem("Dropdown List", null, enumTypeFilter);
            enumTypeFilter.TextChanged += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(enumTypeFilter.Text) && enumTypeFilter.Text.Length > 1)
                {
                    CellDropDownEnumSelector.DropDownItems.Clear();
                    CellDropDownEnumSelector.DropDownItems.Add(enumTypeFilter);
                    enumTypeFilter.Focus();

                    var enumSelectorMenuItems = _enums.Where(it => it.Contains(enumTypeFilter.Text, StringComparison.InvariantCultureIgnoreCase))
                        .Select(it => new ToolStripMenuItem(it, null, OnDropdownCellTypeSelection));

                    foreach (var item in enumSelectorMenuItems)
                    {
                        CellDropDownEnumSelector.DropDownItems.Add(item);
                    }
                }
            };


            CellCtxMenu.Items.AddRange(new []
            {
                new ToolStripMenuItem("Cell Type", null,
                    new ToolStripMenuItem("Plain", null, OnCellTypeMenuClick),
                    new ToolStripMenuItem("Checkbox", null, OnCellTypeMenuClick),
                    new ToolStripMenuItem("Radio Button", null, OnCellTypeMenuClick),
                    new ToolStripMenuItem("Horizontal Bar", null, OnCellTypeMenuClick),
                    new ToolStripMenuItem("Vertical Bar", null, OnCellTypeMenuClick),
                    new ToolStripMenuItem("Button", null, OnCellTypeMenuClick),
                    CellDropDownEnumSelector
                ),
                new ToolStripMenuItem("Merge", null, OnCellFunctionMenuClick),
                new ToolStripMenuItem("Unmerge", null, OnCellFunctionMenuClick),
                new ToolStripMenuItem("Freeze Top", null, OnCellFunctionMenuClick),
                new ToolStripMenuItem("Freeze Left", null, OnCellFunctionMenuClick),
                new ToolStripMenuItem("Freeze Top-left", null, OnCellFunctionMenuClick),
                new ToolStripMenuItem("Unfreeze", null, OnCellFunctionMenuClick)
            });
            grid.ContextMenuStrip = CellCtxMenu;
            ManageCurrentWorksheetEvents();
        }

        private void FakeDocking()
        {
            grid.Width = Width;
            grid.Top = CellValue.Height;
            grid.Height = Height - grid.Top;
        }

        private (ReferenceRange cells, RangePosition range) GetSelectedCells()
        {
            var currsel = grid.CurrentWorksheet.SelectionRange;
            var activecells = grid.CurrentWorksheet.Ranges[currsel];
            return (activecells, currsel);
        }

        public void Evaluate(int SpreadMax)
        {
            var dodrag = _dragging && Math.Abs(_accMouseY) > FDotsPerStep[0];
            if (dodrag)
            {
                var selected = GetSelectedCells();

                //var partialgrid = grid.CurrentWorksheet.GetPartialGrid(currsel);
                var acc = Math.Round((double)_accMouseY / FDotsPerStep[0]);

                if (_dragSlow) acc *= FSlowDragSpeed[0];
                else if (_dragFast) acc *= FFastDragSpeed[0];
                else acc *= FNormDragSpeed[0];

                foreach (var cell in selected.cells.Cells)
                {
                    var isval = double.TryParse(cell.Data?.ToString() ?? "0", out var currval);
                    //cell.DataFormat = CellDataFormatFlag.Number;
                    if (!isval) currval = 0.0;
                    cell.Data = currval + acc;
                }

                _accMouseY = 0;
                _dragChangedVal = true;

                _prevMouseY = _dragInitY;
                SetCursorPos(_dragInitCursorPos.X, _dragInitCursorPos.Y);
            }

            FWorksheet.Stream.IsChanged = false;

            if (FWorkbookBehaviors.IsChanged)
            {
                _isChanged = true;
                foreach (var behavior in FWorkbookBehaviors)
                {
                    behavior?.ExternalAction(grid, this);
                }
            }

            if (_isChanged || dodrag || FWorksheet[0] == null)
            {
                FWorksheet.SliceCount = FNames.SliceCount = grid.Worksheets.Count;
                for (int i = 0; i < grid.Worksheets.Count; i++)
                {
                    FWorksheet[i] = grid.Worksheets[i];
                    FNames[i] = grid.Worksheets[i].Name;
                }
                FWorksheet.Stream.IsChanged = true;
                _isChanged = false;
            }
        }

        public void OnImportsSatisfied()
        {
            FWorkbookData.Changed += spread =>
            {
                if (string.IsNullOrWhiteSpace(FWorkbookData[0])) return;
                if (_initLoaded) return;
                _loading = true;
                try
                {
                    grid.RemoveWorksheet(0);
                    Base64Load(FWorkbookData[0]);

                    FWorksheet.SliceCount = grid.Worksheets.Count;
                    FXml.SliceCount = grid.Worksheets.Count;
                    for (int i = 0; i < grid.Worksheets.Count; i++)
                    {
                        if(FWorksheet[i] == null) FWorksheet[i] = grid.Worksheets[i];
                        if(!_wsxdata.ContainsKey(FWorksheet[i])) continue;
                        FXml[i] = _wsxdata[FWorksheet[i]].Xml;
                    }

                    _initLoaded = true;
                }
                catch (Exception e)
                {
                    _initLoaded = true;
                }
                _loading = false;
                _isChanged = true;
                ManageCurrentWorksheetEvents();
            };
            grid.ActionPerformed += (sender, args) => SaveChanges();
            grid.WorksheetCreated += (sender, args) => SaveChanges();
            grid.WorksheetInserted += (sender, args) => SaveChanges();
            grid.WorksheetNameChanged += (sender, args) => SaveChanges();
            grid.WorksheetRemoved += (sender, args) => SaveChanges();
            grid.CurrentWorksheetChanged += (sender, args) => ManageCurrentWorksheetEvents();

            PopulateEnumList();
            Hde.EnumChanged += (sender, args) => PopulateEnumList();
        }

        private void PopulateEnumList()
        {
            var enumCount = EnumManager.GetEnumEntryCount("AllEnums");
            _enums?.Clear();
            for (int i = 0; i < enumCount; i++)
            {
                var enumentry = EnumManager.GetEnumEntryString("AllEnums", i);
                _enums.Add(enumentry);
            }

            foreach (var ws in grid.Worksheets)
            {
                AddWorksheetData(ws, data =>
                {
                    foreach (var rc in data.EnumCells.Keys)
                    {
                        var cell = data.Worksheet.Cells[rc.r, rc.c];
                        var enumname = data.EnumCells[rc];
                        cell.Body = new DropdownListCell(GetEnumEntries(enumname));
                    }
                });
            }
        }

        public static LinkedList<string> GetEnumEntries(string enumName)
        {
            var entries = new LinkedList<string>();
            for (int ei = 0; ei < EnumManager.GetEnumEntryCount(enumName); ei++)
            {
                entries.AddLast(EnumManager.GetEnumEntryString(enumName, ei));
            }
            return entries;
        }

        private void ManageCurrentWorksheetEvents()
        {
            if (_prevWorksheet != null)
            {
                _prevWorksheet.CellMouseDown -= OnMouseDown;
                _prevWorksheet.CellMouseUp -= OnMouseUp;
                _prevWorksheet.CellMouseMove -= OnMouseMove;
                _prevWorksheet.AfterCellKeyDown -= OnKeyDown;
                _prevWorksheet.CellKeyUp -= OnKeyUp;

                _prevWorksheet.AfterCellEdit -= OnAfterCellEdit;
                _prevWorksheet.BeforeCellEdit -= OnBeforeCellEdit;
                _prevWorksheet.SelectionRangeChanged -= OnSelectionRangeChanged;
            }

            _prevWorksheet = grid.CurrentWorksheet;
            
            _prevWorksheet.CellMouseDown += OnMouseDown;
            _prevWorksheet.CellMouseUp += OnMouseUp;
            _prevWorksheet.CellMouseMove += OnMouseMove;
            _prevWorksheet.AfterCellKeyDown += OnKeyDown;
            _prevWorksheet.CellKeyUp += OnKeyUp;

            _prevWorksheet.AfterCellEdit += OnAfterCellEdit;
            _prevWorksheet.BeforeCellEdit += OnBeforeCellEdit;
            _prevWorksheet.SelectionRangeChanged += OnSelectionRangeChanged;
        }

        private void OnSelectionRangeChanged(object o, RangeEventArgs args)
        {
            var selected = GetSelectedCells();
            CellValue.Text = GetCellTextOrFormula(selected.cells.Cells[selected.range.StartPos]);
            //CellValue.Focus();
        }

        private void OnBeforeCellEdit(object o, CellBeforeEditEventArgs args)
        {
            // TODO: eh
        }

        private void OnAfterCellEdit(object o, CellAfterEditEventArgs args)
        {
            //if (_isEnteringNumber)
            //{

            var isnumber = double.TryParse(
                args.NewData.ToString(),
                NumberStyles.Number ^ NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var cellnum);

            if (isnumber) args.NewData = args.NewData.ToString().Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
            //if (isnumber) args.NewData = cellnum;
            //}
            _isEnteringNumber = false;
            _isEnteringText = false;
        }

        public void SaveChanges()
        {
            if(_loading) return;
            _initLoaded = true;
            FWorkbookData[0] = Base64Save();

            FXml.SliceCount = FWorksheet.SliceCount;
            for (int i = 0; i < FWorksheet.SliceCount; i++)
            {
                if (!_wsxdata.ContainsKey(FWorksheet[i])) continue;
                FXml[i] = _wsxdata[FWorksheet[i]].Xml;
            }

            _isChanged = true;
        }

        private void OnMouseDown(object sender, CellMouseEventArgs e)
        {
            if (e.Buttons == unvell.ReoGrid.Interaction.MouseButtons.Middle)
            {
                var selected = GetSelectedCells();

                GetCursorPos(out _dragInitCursorPos);
                
                _dragInitY = e.AbsolutePosition.Y;
                _dragging = true;
                _prevMouseY = e.AbsolutePosition.Y;
            }
        }
        private void OnMouseUp(object sender, CellMouseEventArgs e)
        {
            if (e.Buttons == unvell.ReoGrid.Interaction.MouseButtons.Middle)
            {
                if (_dragChangedVal)
                {
                    SaveChanges();
                }
                _dragging = false;
                _dragChangedVal = false;
                _prevMouseY = -1;
            }
        }

        private void OnMouseMove(object sender, CellMouseEventArgs e)
        {
            if (!_dragging) return;
            var curry = e.AbsolutePosition.Y;
            _accMouseY -= curry - _prevMouseY;
            _prevMouseY = curry;
        }

        private void OnKeyDown(object sender, AfterCellKeyDownEventArgs e)
        {
            //FKc[0] = (uint) e.KeyCode;
            if ((e.KeyCode & KeyCode.ShiftKey) == KeyCode.ShiftKey) _dragFast = true;
            if ((e.KeyCode & KeyCode.ControlKey) == KeyCode.ControlKey) _dragSlow = true;

            if (grid.CurrentWorksheet.IsEditing)
            {
                if (!_isEnteringText &&
                    (e.KeyCode == KeyCode.D0 ||
                     e.KeyCode == KeyCode.D1 ||
                     e.KeyCode == KeyCode.D2 ||
                     e.KeyCode == KeyCode.D3 ||
                     e.KeyCode == KeyCode.D4 ||
                     e.KeyCode == KeyCode.D5 ||
                     e.KeyCode == KeyCode.D6 ||
                     e.KeyCode == KeyCode.D7 ||
                     e.KeyCode == KeyCode.D8 ||
                     e.KeyCode == KeyCode.D9 ||
                     e.KeyCode == KeyCode.NumPad0 ||
                     e.KeyCode == KeyCode.NumPad1 ||
                     e.KeyCode == KeyCode.NumPad2 ||
                     e.KeyCode == KeyCode.NumPad3 ||
                     e.KeyCode == KeyCode.NumPad4 ||
                     e.KeyCode == KeyCode.NumPad5 ||
                     e.KeyCode == KeyCode.NumPad6 ||
                     e.KeyCode == KeyCode.NumPad7 ||
                     e.KeyCode == KeyCode.NumPad8 ||
                     e.KeyCode == KeyCode.NumPad9)
                )
                {
                    _isEnteringNumber = true;
                }
                else _isEnteringText = true;
                return;
            }

            var currsel = grid.CurrentWorksheet.SelectionRange;
            if (e.KeyCode == KeyCode.M)
            {
                grid.DoAction(new MergeRangeAction(currsel));
            }
            if (e.KeyCode == KeyCode.U)
            {
                grid.DoAction(new UnmergeRangeAction(currsel));
            }

            if (e.KeyCode == KeyCode.F5)
            {
                grid.Undo();
            }
            if (e.KeyCode == KeyCode.F6)
            {
                grid.Redo();
            }
            if (e.KeyCode == KeyCode.F9)
            {
                grid.CurrentWorksheet.FreezeToCell(currsel.EndRow + 1, currsel.EndCol + 1, FreezeArea.Top);
            }
            if (e.KeyCode == KeyCode.F10)
            {
                grid.CurrentWorksheet.FreezeToCell(currsel.EndRow + 1, currsel.EndCol + 1, FreezeArea.Left);
            }
            if (e.KeyCode == KeyCode.F11)
            {
                grid.CurrentWorksheet.FreezeToCell(currsel.EndRow + 1, currsel.EndCol + 1, FreezeArea.LeftTop);
            }
            if (e.KeyCode == KeyCode.F12)
            {
                grid.CurrentWorksheet.Unfreeze();
            }

            if (e.KeyCode == KeyCode.Add)
            {
                grid.DoAction(new InsertRowsAction(currsel.Row, currsel.Rows));
            }
            if (e.KeyCode == KeyCode.Subtract)
            {
                grid.DoAction(new RemoveRowsAction(currsel.Row, currsel.Rows));
            }
            if (e.KeyCode == KeyCode.Multiply)
            {
                grid.DoAction(new InsertColumnsAction(currsel.Col, currsel.Cols));
            }
            if (e.KeyCode == KeyCode.Divide)
            {
                grid.DoAction(new RemoveColumnsAction(currsel.Col, currsel.Cols));
            }
        }

        private void OnKeyUp(object sender, CellKeyDownEventArgs e)
        {
            if ((e.KeyCode & KeyCode.ShiftKey) == KeyCode.ShiftKey) _dragFast = false;
            if ((e.KeyCode & KeyCode.ControlKey) == KeyCode.ControlKey) _dragSlow = false;
        }

        private void OnResize(object sender, EventArgs e)
        {
            FakeDocking();
        }

        private string GetCellTextOrFormula(Cell cell)
        {
            if (cell.HasFormula)
            {
                return "=" + cell.Formula;
            }
            else
            {
                return cell.DisplayText;
            }
        }

        private void OnCellValueKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var selected = GetSelectedCells();
                var isnumber = double.TryParse(CellValue.Text, out var cellnum);
                if(isnumber) grid.DoAction(new SetCellDataAction(selected.range.StartPos, cellnum));
                else grid.DoAction(new SetCellDataAction(selected.range.StartPos, CellValue.Text));
            }

            if (e.KeyCode == Keys.Escape)
            {
                var selected = GetSelectedCells();
                CellValue.Text = GetCellTextOrFormula(selected.cells.Cells[selected.range.StartPos]);
            }
        }

        private void OnCellValueFocus(object sender, EventArgs e)
        {
            CellValue.SelectAll();
        }
    }
}
