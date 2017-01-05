using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
    public struct Rect
    {
        public int ID;
        public double X;
        public double Y;
        public double W;
        public double H;

        public Rect(double x, double y, double w, double h, int id)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            ID = id;
        }

        public double Area => W * H;
    }
    public class RectanglePacker
    {
        public double Width { get; private set; }
        public double Height { get; private set; }

        List<Node> nodes = new List<Node>();

        public RectanglePacker()
        {
            nodes.Add(new Node(0, 0, double.MaxValue, double.MaxValue));
        }
        public RectanglePacker(double maxw, double maxh)
        {
            nodes.Add(new Node(0, 0, maxw, maxh));
        }

        public bool Pack(double w, double h, out double x, out double y)
        {
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (w <= nodes[i].W && h <= nodes[i].H)
                {
                    var node = nodes[i];
                    nodes.RemoveAt(i);
                    x = node.X;
                    y = node.Y;
                    double r = x + w;
                    double b = y + h;
                    nodes.Add(new Node(r, y, node.Right - r, h));
                    nodes.Add(new Node(x, b, w, node.Bottom - b));
                    nodes.Add(new Node(r, b, node.Right - r, node.Bottom - b));
                    Width = Math.Max(Width, r);
                    Height = Math.Max(Height, b);
                    return true;
                }
            }
            x = 0;
            y = 0;
            return false;
        }

        public struct Node
        {
            public double X;
            public double Y;
            public double W;
            public double H;

            public Node(double x, double y, double w, double h)
            {
                X = x;
                Y = y;
                W = w;
                H = h;
            }

            public double Right => X + W;
            public double Bottom => Y + H;
        }
    }
    [PluginInfo(Name = "RectPack",
        Category = "2d",
        Version = "ChevyRay",
        Author = "microdee"
        )]
    public class ChevyRayRectanglePackerNode : IPluginEvaluate
    {
        [Input("Rectangle ", DimensionNames = new[] { "W", "H" }, DefaultValues = new[] { 100.0, 100.0 })]
        public IDiffSpread<Vector2D> FRectangle;
        [Input("Gap ")]
        public IDiffSpread<Vector2D> FGap;
        [Output("Packed Rectangles ", DimensionNames = new[] { "X", "Y", "W", "H" })]
        public ISpread<Vector4D> FPacked;
        [Output("Former Index")]
        public ISpread<int> FID;
        [Output("Dimensions ", DimensionNames = new[] { "W", "H" })]
        public ISpread<Vector2D> FDim;
        [Output("Success")]
        public ISpread<bool> FSuccess;

        public void Evaluate(int SpreadMax)
        {
            if (FRectangle.IsChanged || FGap.IsChanged)
            {
                FPacked.SliceCount = FRectangle.SliceCount;
                FID.SliceCount = FRectangle.SliceCount;
                var rectlist = FRectangle.Select((wh, i) => new Rect(0, 0, wh.x, wh.y, i)).ToList();
                rectlist.Sort((a, b) => b.Area.CompareTo(a.Area));

                var packer = new RectanglePacker();
                double dimw = 0;
                double dimh = 0;
                for (int i = 0; i < rectlist.Count; i++)
                {
                    var rect = rectlist[i];
                    FSuccess[0] = packer.Pack(rect.W + FGap[0].x, rect.H + FGap[0].y, out rect.X, out rect.Y);
                    rectlist[i] = rect;
                    FID[i] = rect.ID;
                    FPacked[i] = new Vector4D(rect.X, rect.Y, rect.W, rect.H);
                    dimw = Math.Max(dimw, rect.X + rect.W);
                    dimh = Math.Max(dimh, rect.Y + rect.H);
                }
                FDim[0] = new Vector2D(dimw, dimh);
            }
        }
    }
}
