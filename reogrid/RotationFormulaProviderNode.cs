using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid;
using unvell.ReoGrid.Formula;
using unvell.ReoGrid.Utility;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.reogrid
{
    [PluginInfo(
        Name = "Rotation",
        Category = "ReoGrid.FormulaProvider",
        Version = "Quaternion",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class RotationFormulaProviderNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        private float GetQuatComponent(object[] args, Quaternion q)
        {
            var selcomp = (int)(double)args[0] % 4;
            switch (selcomp)
            {
                case 0: return q.X;
                case 1: return q.Y;
                case 2: return q.Z;
                case 3: return q.W;
                default: return 0;
            }
        }

        public void OnImportsSatisfied()
        {
            if(FormulaExtension.CustomFunctions.ContainsKey("qEuler")) return;
            FormulaExtension.CustomFunctions["qEuler"] = (cell, args) =>
            {
                if (args.Length < 2) return null;

                var eulerangles = new float[3];
                if (args[1] is RangePosition range)
                {
                    var i = 0;
                    cell.Worksheet.IterateCells(range, (r, c, icell) =>
                    {
                        if (i >= 3) return false;
                        var isval = CellUtility.TryGetNumberData(icell, out var comp);
                        eulerangles[i] = isval ? (float)(comp * Math.PI * 2) : 0;
                        i++;
                        return true;
                    });
                }
                else
                {
                    if (args.Length < 4) return null;
                    for (int i = 0; i < 3; i++)
                    {
                        eulerangles[i] = (float)(double)args[i + 1];
                    }
                }
                var res = Quaternion.CreateFromYawPitchRoll(eulerangles[1], eulerangles[0], eulerangles[2]);
                return GetQuatComponent(args, res);
            };
            FormulaExtension.CustomFunctions["qEulerOrdered"] = (cell, args) =>
            {
                if (args.Length < 5) return null;

                var eulerangles = new float[3];
                if (args[4] is RangePosition range)
                {
                    var i = 0;
                    cell.Worksheet.IterateCells(range, (r, c, icell) =>
                    {
                        if (i >= 3) return false;
                        var isval = CellUtility.TryGetNumberData(icell, out var comp);
                        eulerangles[i] = isval ? (float)(comp * Math.PI * 2) : 0;
                        i++;
                        return true;
                    });
                }
                else
                {
                    if (args.Length < 7) return null;
                    for (int i = 0; i < 3; i++)
                    {
                        eulerangles[i] = (float)(double)args[i + 4];
                    }
                }

                var res = Quaternion.Identity;
                for (int i = 0; i < 3; i++)
                {
                    var selcomp = (int)(double)args[i+1] % 3;
                    switch (selcomp)
                    {
                        case 0: res = res * Quaternion.CreateFromYawPitchRoll(0, eulerangles[i], 0); break;
                        case 1: res = res * Quaternion.CreateFromYawPitchRoll(eulerangles[i], 0, 0); break;
                        case 2: res = res * Quaternion.CreateFromYawPitchRoll(0, 0, eulerangles[i]); break;
                    }
                }
                return GetQuatComponent(args, res);
            };
        }

        public void Evaluate(int SpreadMax) { }
    }
}
