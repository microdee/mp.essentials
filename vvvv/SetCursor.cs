using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.vvvv
{
    public enum CursorSelection
    {
        Arrow,
        AppStarting,
        Cross,
        Default,
        Hand,
        Help,
        HSplit,
        VSplit,
        IBeam,
        No,
        NoMove2D,
        NoMoveHoriz,
        NoMoveVert,
        PanNorth,
        PanNE,
        PanEast,
        PanSE,
        PanSouth,
        PanSW,
        PanWest,
        PanNW,
        SizeAll,
        SizeNS,
        SizeWE,
        SizeNESW,
        SizeNWSE,
        UpArrow,
        WaitCursor,
        None,
        Custom
    }

    [PluginInfo(
        Name = "SetCursor",
        Category = "Control",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class SetCursorNode : IPartImportsSatisfiedNotification, IPluginEvaluate
    {
        [Input("Control")]
        public Pin<Control> FInControl;

        [Input("Cursor")]
        public ISpread<CursorSelection> FCursorSel;
        [Input("Custom Cursor")]
        public ISpread<Cursor> FCursor;
        [Input("Set", IsBang = true)]
        public ISpread<bool> FSet;

        private readonly Cursor _blankCursor = new Cursor(Path.Combine(VersionWriter.PackDir, "blank.cur"));


        public void OnImportsSatisfied()
        {

        }

        public void Evaluate(int SpreadMax)
        {
            if (FInControl[0] == null) return;
            if (!FSet[0]) return;
            switch (FCursorSel[0])
            {
                case CursorSelection.Arrow:
                    FInControl[0].Cursor = Cursors.Arrow;
                    break;
                case CursorSelection.AppStarting:
                    FInControl[0].Cursor = Cursors.AppStarting;
                    break;
                case CursorSelection.Cross:
                    FInControl[0].Cursor = Cursors.Cross;
                    break;
                case CursorSelection.Default:
                    FInControl[0].Cursor = Cursors.Default;
                    break;
                case CursorSelection.Hand:
                    FInControl[0].Cursor = Cursors.Hand;
                    break;
                case CursorSelection.Help:
                    FInControl[0].Cursor = Cursors.Help;
                    break;
                case CursorSelection.HSplit:
                    FInControl[0].Cursor = Cursors.HSplit;
                    break;
                case CursorSelection.IBeam:
                    FInControl[0].Cursor = Cursors.IBeam;
                    break;
                case CursorSelection.No:
                    FInControl[0].Cursor = Cursors.No;
                    break;
                case CursorSelection.NoMove2D:
                    FInControl[0].Cursor = Cursors.NoMove2D;
                    break;
                case CursorSelection.NoMoveHoriz:
                    FInControl[0].Cursor = Cursors.NoMoveHoriz;
                    break;
                case CursorSelection.NoMoveVert:
                    FInControl[0].Cursor = Cursors.NoMoveVert;
                    break;
                case CursorSelection.PanNorth:
                    FInControl[0].Cursor = Cursors.PanNorth;
                    break;
                case CursorSelection.PanNE:
                    FInControl[0].Cursor = Cursors.PanNE;
                    break;
                case CursorSelection.PanEast:
                    FInControl[0].Cursor = Cursors.PanEast;
                    break;
                case CursorSelection.PanSE:
                    FInControl[0].Cursor = Cursors.PanSE;
                    break;
                case CursorSelection.PanSouth:
                    FInControl[0].Cursor = Cursors.PanSouth;
                    break;
                case CursorSelection.PanSW:
                    FInControl[0].Cursor = Cursors.PanSW;
                    break;
                case CursorSelection.PanWest:
                    FInControl[0].Cursor = Cursors.PanWest;
                    break;
                case CursorSelection.PanNW:
                    FInControl[0].Cursor = Cursors.PanNW;
                    break;
                case CursorSelection.SizeAll:
                    FInControl[0].Cursor = Cursors.SizeAll;
                    break;
                case CursorSelection.SizeNS:
                    FInControl[0].Cursor = Cursors.SizeNS;
                    break;
                case CursorSelection.SizeWE:
                    FInControl[0].Cursor = Cursors.SizeWE;
                    break;
                case CursorSelection.SizeNESW:
                    FInControl[0].Cursor = Cursors.SizeNESW;
                    break;
                case CursorSelection.SizeNWSE:
                    FInControl[0].Cursor = Cursors.SizeNWSE;
                    break;
                case CursorSelection.UpArrow:
                    FInControl[0].Cursor = Cursors.UpArrow;
                    break;
                case CursorSelection.VSplit:
                    FInControl[0].Cursor = Cursors.VSplit;
                    break;
                case CursorSelection.WaitCursor:
                    FInControl[0].Cursor = Cursors.WaitCursor;
                    break;
                case CursorSelection.None:
                    FInControl[0].Cursor = _blankCursor;
                    break;
                case CursorSelection.Custom:
                    if(FCursor[0] == null) break;
                    FInControl[0].Cursor = FCursor[0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
