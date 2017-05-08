using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GraphSharp;
using GraphSharp.Algorithms.Layout.Compound;
using GraphSharp.Algorithms.Layout.Compound.FDP;
using GraphSharp.Algorithms.Layout.Simple.FDP;
using QuickGraph;
using QuickGraph.Algorithms;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes
{
    [PluginInfo(
        Name = "FR",
        Category = "GraphLayout",
        Author = "microdee"
    )]
    public class FrGraphlayoutNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        public IIOFactory FIoFactory;

        private PinDictionary pd;

        [Input("Vertices", DefaultValue = 2)]
        public ISpread<string> FVertices;
        [Input("Edge From")]
        public ISpread<string> FEdgeFrom;
        [Input("Edge To")]
        public ISpread<string> FEdgeTo;

        [Input("Vertex Size")]
        public ISpread<double> FVSize;
        [Input("Vertex Type", DefaultEnumEntry = "Automatic")]
        public ISpread<CompoundVertexInnerLayoutType> FVType;
        [Input("Initial Vertex Position")]
        public ISpread<Vector2D> FVPos;

        [Input("Async")]
        public ISpread<bool> FAsync;
        [Input("Build", IsBang = true)]
        public ISpread<bool> FBuild;

        [Output("Position")]
        public ISpread<Vector2D> FPosOut;
        [Output("Status")]
        public ISpread<string> FState;

        private BidirectionalGraph<string, IEdge<string>> graph = new BidirectionalGraph<string, IEdge<string>>(true);
        private FRLayoutAlgorithm<string, IEdge<string>, BidirectionalGraph<string, IEdge<string>>> layout;

        private Dictionary<string, Size> VSize = new Dictionary<string, Size>();
        private Dictionary<string, Thickness> VThickness = new Dictionary<string, Thickness>();
        private Dictionary<string, CompoundVertexInnerLayoutType> VType = new Dictionary<string, CompoundVertexInnerLayoutType>();
        private Dictionary<string, Point> VPos = new Dictionary<string, Point>();
        private List<string> VCache = new List<string>();

        public void OnImportsSatisfied()
        {
            pd = new PinDictionary(FIoFactory);
            foreach (var prop in typeof(FreeFRLayoutParameters).GetProperties())
            {
                pd.AddInput(prop.PropertyType, new InputAttribute(prop.Name));
            }
        }

        private bool init = false;
        public void Evaluate(int SpreadMax)
        {
            if (FBuild[0] || (pd.InputChanged && init))
            {
                graph.Clear();
                VSize.Clear();
                VThickness.Clear();
                VType.Clear();
                VPos.Clear();
                VCache.Clear();

                for (int i = 0; i < FVertices.SliceCount; i++)
                {
                    graph.AddVertex(FVertices[i]);
                    VSize.Add(FVertices[i], new Size(FVSize[i], FVSize[i]));
                    VThickness.Add(FVertices[i], new Thickness(1.0));
                    VType.Add(FVertices[i], FVType[i]);
                    VPos.Add(FVertices[i], new Point(FVPos[i].x, FVPos[i].y));
                    VCache.Add(FVertices[i]);
                }
                for (int i = 0; i < Math.Max(FEdgeFrom.SliceCount, FEdgeTo.SliceCount); i++)
                    graph.AddEdge(new Edge<string>(FEdgeFrom[i], FEdgeTo[i]));

                var cparams = new FreeFRLayoutParameters();
                foreach (var prop in typeof(FreeFRLayoutParameters).GetProperties())
                {
                    var input = pd.InputPins[prop.Name].Spread[0];
                    if (!(input.ToString().StartsWith("0") && input.ToString().EndsWith("0")))
                        prop.SetValue(cparams, pd.InputPins[prop.Name].Spread[0]);
                }
                layout = new FRLayoutAlgorithm<string, IEdge<string>, BidirectionalGraph<string, IEdge<string>>>(graph, VPos, cparams);
                foreach (var prop in typeof(FreeFRLayoutParameters).GetProperties())
                {
                    var input = pd.InputPins[prop.Name].Spread[0];
                    if (!(input.ToString().StartsWith("0") && input.ToString().EndsWith("0")))
                        prop.SetValue(layout.Parameters, pd.InputPins[prop.Name].Spread[0]);
                }
                layout.Finished += (sender, args) =>
                {
                    FPosOut.SliceCount = VCache.Count;
                    for (int i = 0; i < VCache.Count; i++)
                    {
                        var point = layout.VertexPositions[VCache[i]];
                        FPosOut[i] = new Vector2D(point.X, point.Y);
                    }
                };
                if (FAsync[0])
                {
                    Task.Run(() =>
                    {
                        layout.Compute();
                    });
                }
                else layout.Compute();
                init = true;
            }
            if (layout != null) FState[0] = layout.State.ToString();
        }
    }
}
