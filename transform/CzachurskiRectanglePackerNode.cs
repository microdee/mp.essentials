using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Transform
{
    public static class RectPackHelper
    {
        public static bool EqE(double a, double b, double e)
        {
            return Math.Abs(a - b) < e;
        }
        public static double RectWhArea(this Vector2D vin)
        {
            return vin.x*vin.y;
        }
        public static double RectWhPerimeter(this Vector2D vin)
        {
            return vin.x*2 + vin.y*2;
        }

        public static int RectFits(this Vector2D vin, Vector2D bigger)
        {
            if (EqE(vin.x, bigger.x, 0.00001) && EqE(vin.y, bigger.y, 0.00001)) return 3;
            if (EqE(vin.x, bigger.y, 0.00001) && EqE(vin.y, bigger.x, 0.00001)) return 4;
            if (vin.x <= bigger.x && vin.y <= bigger.y) return 1;
            if (vin.x <= bigger.y && vin.y <= bigger.x) return 2;
            return 0;
        }
        public static int RectFits(this Vector2D vin, RectClass bigger)
        {
            return RectFits(vin, new Vector2D(bigger.Width(), bigger.Height()));
        }

        public static Vector2D PackBin(RectXywhFlipped[] vlist, double maxside, double discardbelow, List<RectXywhFlipped> succ, List<RectXywhFlipped> unsucc)
        {
            RectNode root = new RectNode();
            var funcs = CompFuncs();
            var ordered = new List<List<RectXywhFlipped>>();

            for (int f = 0; f < funcs.Count; f++)
            {
                var ii = f;
                var clist = ((RectXywhFlipped[]) vlist.Clone()).ToList();
                ordered.Add(clist);
                clist.Sort((a, b) => funcs[ii](a, b));
                clist.Reverse();
            }

            var minbin = new RectXywh
            {
                Left = 0,
                Top = 0,
                FWidth = maxside,
                FHeight = maxside
            };

            var minfunc = -1;
            var bestfunc = 0;
            var bestarea = 0.0;
            var area = 0.0;
            var step = 0.0;
            int fit = 0;
            bool fail = false;

            for (int f = 0; f < funcs.Count; f++)
            {
                var v = ordered[f];
                step = minbin.Width() / 2;
                root.Reset(minbin);
                while (true)
                {
                    if (root.Rectangle.Width() > minbin.Width())
                    {
                        if (minfunc > -1) break;
                        area = 0;
                        root.Reset(minbin);
                        for (int i = 0; i < vlist.Length; i++)
                        {
                            if (root.Insert(v[i]) != null)
                            {
                                area += v[i].Area();
                            }
                        }
                        fail = true;
                        break;
                    }
                    fit = -1;
                    for (int i = 0; i < vlist.Length; i++)
                    {
                        if (root.Insert(v[i]) == null)
                        {
                            fit = 1;
                            break;
                        }
                    }
                    if (fit == -1 && step <= discardbelow) break;

                    root.Reset(new RectXywh
                    {
                        FWidth = root.Rectangle.Width() + fit*step,
                        FHeight = root.Rectangle.Height() + fit*step,
                        Top = 0,
                        Left = 0
                    });
                    step /= 2;
                    step = Math.Max(step, 0.0001);
                }
                if (!fail && (minbin.Area() >= root.Rectangle.Area()))
                {
                    minbin = new RectXywh
                    {
                        FWidth = root.Rectangle.Width(),
                        FHeight = root.Rectangle.Height(),
                        Top = 0, Left = 0
                    };
                    minfunc = f;
                }
                else if (fail && (area > bestarea))
                {
                    bestarea = area;
                    bestfunc = f;
                }
                fail = false;
            }

            var sv = ordered[minfunc == -1 ? bestfunc : minfunc];

            double clipx = 0.0, clipy = 0.0;
            RectNode ret;

            root.Reset(minbin);

            for (int i = 0; i < vlist.Length; i++)
            {
                ret = root.Insert(sv[i]);
                if (ret != null)
                {
                    sv[i].Left = ret.Rectangle.Left;
                    sv[i].Top = ret.Rectangle.Top;

                    if (sv[i].Flipped)
                    {
                        sv[i].Flipped = false;
                        sv[i].Flip();
                    }

                    clipx = Math.Max(clipx, ret.Rectangle.Right());
                    clipy = Math.Max(clipy, ret.Rectangle.Bottom());
                    succ.Add(sv[i]);
                }
                else
                {
                    unsucc.Add(sv[i]);
                    sv[i].Flipped = false;
                }
            }
            return new Vector2D(clipx, clipy);
        }

        public static bool Pack(RectXywhFlipped[] vlist, double sidemax, double discardbelow, List<RectBin> bins, int maxbins)
        {
            var rect = new RectXywh
            {
                FWidth = sidemax,
                FHeight = sidemax,
                Top = 0, Left = 0
            };
            foreach (RectXywhFlipped r in vlist)
            {
                if (r.Fits(rect) == 0) return false;
            }
            List<RectXywhFlipped> unsucc = vlist.ToList();
            int ii = 0;
            while (true)
            {
                if (unsucc.Count > 0 && ii < maxbins)
                {
                    var newbin = new RectBin();
                    var listin = unsucc.ToArray();
                    unsucc.Clear();
                    newbin.Size = PackBin(listin, sidemax, discardbelow, newbin.Rects, unsucc);
                    bins.Add(newbin);
                }
                else break;
                ii++;
            }
            return true;
        }

        public static int CompArea(RectClass a, RectClass b)
        {
            return a.Area().CompareTo(b.Area());
        }

        public static int CompPerimeter(RectClass a, RectClass b)
        {
            return a.Perimeter().CompareTo(b.Perimeter());
        }

        public static int CompMaxSide(RectClass a, RectClass b)
        {
            return Math.Max(a.Width(), a.Height()).CompareTo(Math.Max(b.Width(), b.Height()));
        }
        public static int CompMaxWidth(RectClass a, RectClass b)
        {
            return a.Width().CompareTo(b.Width());
        }
        public static int CompMaxHeight(RectClass a, RectClass b)
        {
            return a.Height().CompareTo(b.Height());
        }

        public static List<Func<RectClass, RectClass, int>> CompFuncs()
        {
            return new List<Func<RectClass, RectClass, int>>()
            {
                CompArea,
                CompPerimeter,
                CompMaxSide,
                CompMaxWidth,
                CompMaxHeight
            };
        }
    }
    public abstract class RectClass
    {
        public double Left;
        public double Top;
        public int ID;

        public abstract double Width();
        public abstract double Height();
        public abstract double Right();
        public abstract double Bottom();

        public double Area() { return Width() * Height(); }
        public double Perimeter() { return Width() * 2 + Height() * 2; }

        public int Fits(RectClass bigger)
        {
            if (RectPackHelper.EqE(Width(), bigger.Width(), 0.00001) && RectPackHelper.EqE(Height(), bigger.Height(), 0.00001)) return 3;
            if (RectPackHelper.EqE(Width(), bigger.Height(), 0.00001) && RectPackHelper.EqE(Height(), bigger.Width(), 0.00001)) return 4;
            if (Width() <= bigger.Width() && Height() <= bigger.Height()) return 1;
            if (Width() <= bigger.Height() && Height() <= bigger.Width()) return 2;
            return 0;
        }
    }

    public class RectXywh : RectClass
    {
        public double FWidth;
        public double FHeight;

        public override double Width() { return FWidth; }
        public override double Height() { return FHeight; }
        public override double Right() { return Left + FWidth; }
        public override double Bottom() { return Top + FHeight; }
    }

    public class RectXywhFlipped : RectXywh
    {
        public void Flip()
        {
            Flipped = !Flipped;
            var ow = FWidth;
            var oh = FHeight;
            FWidth = oh;
            FHeight = ow;
        }

        public bool Flipped;
    }
    public class RectLtrb : RectClass
    {
        public double FRight;
        public double FBottom;

        public override double Width() { return FRight - Left; }
        public override double Height() { return FBottom - Top; }
        public override double Right() { return FRight; }
        public override double Bottom() { return FBottom; }
    }
    public class RectBin
    {
        public Vector2D Size;
        public List<RectXywhFlipped> Rects = new List<RectXywhFlipped>();
    }

    public class RectNode
    {
        public RectLtrb Rectangle;
        public RectPNode[] C = new RectPNode[2];
        public bool Id;

        public void Reset(RectClass rin)
        {
            Id = false;
            Rectangle = new RectLtrb
            {
                Top = 0,
                Left = 0,
                FRight = rin.Width(),
                FBottom = rin.Height()
            };
            DelCheck();
        }

        public RectNode Insert(RectXywhFlipped rin)
        {
            if (C[0] == null) C[0] = new RectPNode();
            if (C[1] == null) C[1] = new RectPNode();
            if ((C[0].PNode != null) && C[0].Fill)
            {
                var newn = C[0].PNode.Insert(rin);
                return newn ?? C[1].PNode.Insert(rin);
            }
            if (Id) return null;
            var fit = rin.Fits(Rectangle);
            switch (fit)
            {
                case 0:
                    return null;
                case 1:
                    rin.Flipped = false;
                    break;
                case 2:
                    rin.Flipped = true;
                    break;
                case 3:
                    Id = true;
                    rin.Flipped = false;
                    return this;
                case 4:
                    Id = true;
                    rin.Flipped = true;
                    return this;
            }

            var iw = rin.Flipped ? rin.Height() : rin.Width();
            var ih = rin.Flipped ? rin.Width() : rin.Height();

            if (Rectangle.Width() - iw > Rectangle.Height() - ih)
            {
                C[0].Set(Rectangle.Left, Rectangle.Top, Rectangle.Left + iw, Rectangle.Bottom());
                C[1].Set(Rectangle.Left + iw, Rectangle.Top, Rectangle.Right(), Rectangle.Bottom());
            }
            else
            {
                C[0].Set(Rectangle.Left, Rectangle.Top, Rectangle.Right(), Rectangle.Top + ih);
                C[1].Set(Rectangle.Left, Rectangle.Top + ih, Rectangle.Right(), Rectangle.Bottom());
            }

            return C[0].PNode.Insert(rin);
        }

        private void DelCheck()
        {
            if (C[0] != null)
            {
                C[0].Fill = false;
                C[0].PNode?.DelCheck();
            }
            if (C[1] != null)
            {
                C[1].Fill = false;
                C[1].PNode?.DelCheck();
            }
        }
    }
    public class RectPNode
    {
        public RectNode PNode;
        public bool Fill = false;

        public void Set(double l, double t, double r, double b)
        {
            var rect = new RectLtrb
            {
                Left = l,
                Top = t,
                FRight = r,
                FBottom = b
            };
            if (PNode == null) PNode = new RectNode
            {
                Rectangle = rect
            };
            else
            {
                PNode.Rectangle = rect;
                PNode.Id = false;
            }
            Fill = true;
        }
    }

    [PluginInfo(Name = "RectPack",
        Category = "2d",
        Version = "Czachurski",
        Author = "microdee"
        )]
    public class JimScottRectanglePackerNode : IPluginEvaluate
    {
        [Input("Rectangle ", DimensionNames = new[] { "W", "H" }, DefaultValues = new[] { 100.0, 100.0 })]
        public IDiffSpread<Vector2D> FRectangle;
        [Input("Square Box", DefaultValue = 16383.0)]
        public IDiffSpread<double> FBox;
        [Input("Max Bin Count", DefaultValue = 64)]
        public IDiffSpread<int> FMaxBins;
        [Input("Discard Below", DefaultValue = 128)]
        public IDiffSpread<int> FDiscardBelow;

        [Output("Packed Rectangles ", DimensionNames = new[] { "X", "Y", "W", "H" })]
        public ISpread<ISpread<Vector4D>> FPacked;
        [Output("Former Index")]
        public ISpread<ISpread<int>> FID;
        [Output("Rotated")]
        public ISpread<ISpread<bool>> FFlipped;
        [Output("Dimensions ", DimensionNames = new[] { "W", "H" })]
        public ISpread<Vector2D> FDim;
        [Output("Success")]
        public ISpread<bool> FSuccess;

        public void Evaluate(int SpreadMax)
        {
            if (FRectangle.IsChanged || FMaxBins.IsChanged || FDiscardBelow.IsChanged || FBox.IsChanged)
            {
                var binlist = new List<RectBin>();
                var rectlistin = new RectXywhFlipped[FRectangle.SliceCount];
                for (int i = 0; i < FRectangle.SliceCount; i++)
                {
                    rectlistin[i] = new RectXywhFlipped
                    {
                        FWidth = FRectangle[i].x,
                        FHeight = FRectangle[i].y,
                        ID = i,
                        Left = 0, Top = 0, Flipped = false
                    };
                }
                FSuccess[0] = RectPackHelper.Pack(rectlistin, FBox[0], FDiscardBelow[0], binlist, FMaxBins[0]);

                FPacked.SliceCount = binlist.Count;
                FID.SliceCount = binlist.Count;
                FDim.SliceCount = binlist.Count;
                FFlipped.SliceCount = binlist.Count;

                for (int i = 0; i < binlist.Count; i++)
                {
                    FPacked[i].SliceCount = binlist[i].Rects.Count;
                    FID[i].SliceCount = binlist[i].Rects.Count;
                    FFlipped[i].SliceCount = binlist[i].Rects.Count;
                    FDim[i] = binlist[i].Size;

                    for (int j = 0; j < FPacked[i].SliceCount; j++)
                    {
                        var rect = binlist[i].Rects[j];
                        FPacked[i][j] = new Vector4D(rect.Left, rect.Top, rect.Width(), rect.Height());
                        FFlipped[i][j] = rect.Flipped;
                        FID[i][j] = rect.ID;
                    }
                }
            }
        }
    }
}
