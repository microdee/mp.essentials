using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using GraphSharp.Algorithms.Layout;
using QuickGraph;
using VVVV.Nodes.PDDN;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Graphs
{
    public static class GraphHelper
    {
        public static IEdge<TVertex>[] GetConnected<TVertex>(this IVertexAndEdgeListGraph<TVertex, IEdge<TVertex>> graph, TVertex src)
        {
            var res = new List<IEdge<TVertex>>();
            foreach (var edge in graph.Edges)
            {
                if (!edge.Source.Equals(src) && !edge.Target.Equals(src)) continue;
                if(res.Contains(edge)) continue;
                res.Add(edge);
            }
            return res.ToArray();
        }
    }
    public class MessageEdge : IEdge<Message>, IEquatable<MessageEdge>
    {
        public bool Equals(MessageEdge other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Source, other.Source) && Equals(Target, other.Target) && ID == other.ID;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Source != null ? Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ID;
                return hashCode;
            }
        }

        public MessageEdge(Message s, Message t)
        {
            Source = s;
            Target = t;
        }
        public Message Source { get; }
        public Message Target { get; }
        public int ID { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(MessageEdge)) return false;
            return Equals((MessageEdge) obj);
        }
        
    }

    public abstract class AbstractGraphLayoutGeneratorNode<TVertex, TEdge, TGraph, TLayout, TLayoutParams> : IPluginEvaluate, IPartImportsSatisfiedNotification
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : BidirectionalGraph<TVertex, TEdge>
        where TLayoutParams : LayoutParametersBase, new()
        where TLayout : ParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, TLayoutParams>
    {
        [Import]
        public IIOFactory FIoFactory;
        [Import]
        public IHDEHost FHdeHost;

        private PinDictionary pd;

        [Input("Vertices", DefaultValue = 2)]
        public ISpread<TVertex> FVertices;
        [Input("Edge From")]
        public ISpread<TVertex> FEdgeFrom;
        [Input("Edge To")]
        public ISpread<TVertex> FEdgeTo;

        [Input("Vertex Size")]
        public ISpread<double> FVSize;
        //[Input("Vertex Type", DefaultEnumEntry = "Automatic")]
        //public ISpread<CompoundVertexInnerLayoutType> FVType;
        [Input("Initial Vertex Position")]
        public ISpread<Vector2D> FVPos;

        [Input("Async")]
        public ISpread<bool> FAsync;
        [Input("Build", IsBang = true)]
        public ISpread<bool> FBuild;

        [Output("Position")]
        public ISpread<Vector2D> FPosOut;
        [Output("Graph")]
        public ISpread<TGraph> FGraph;
        [Output("Layout")]
        public ISpread<TLayout> FLayout;
        [Output("Status")]
        public ISpread<string> FState;

        protected TGraph graph;
        protected TLayout layout;
        protected Dictionary<TVertex, Size> VSize = new Dictionary<TVertex, Size>();
        protected Dictionary<TVertex, Thickness> VThickness = new Dictionary<TVertex, Thickness>();
        protected Dictionary<TVertex, Point> VPos = new Dictionary<TVertex, Point>();
        protected List<TVertex> VCache = new List<TVertex>();

        protected bool GraphChanged = false;

        public virtual void InitializeNode() { }
        public virtual void InitializeBuild() { }

        public abstract TGraph NewGraph();
        public abstract TLayout NewLayout(TLayoutParams cparams);
        public abstract TEdge NewEdge(TVertex from, TVertex to, int i);

        public void OnImportsSatisfied()
        {
            graph = NewGraph();
            pd = new PinDictionary(FIoFactory);
            foreach (var prop in typeof(TLayoutParams).GetProperties())
            {
                pd.AddInput(prop.PropertyType, new InputAttribute(prop.Name));
            }
        }

        protected bool init = false;
        public void Evaluate(int SpreadMax)
        {
            if (FGraph.Stream.IsChanged) FGraph.Stream.IsChanged = false;
            if (FLayout.Stream.IsChanged) FLayout.Stream.IsChanged = false;
            if (FBuild[0] || (pd.InputChanged && init))
            {
                InitializeBuild();
                graph?.Clear();
                VSize.Clear();
                VThickness.Clear();
                VPos.Clear();
                VCache.Clear();

                for (int i = 0; i < FVertices.SliceCount; i++)
                {
                    graph.AddVertex(FVertices[i]);
                    VSize.Add(FVertices[i], new Size(FVSize[i], FVSize[i]));
                    VThickness.Add(FVertices[i], new Thickness(1.0));
                    VPos.Add(FVertices[i], new Point(FVPos[i].x, FVPos[i].y));
                    VCache.Add(FVertices[i]);
                }
                for (int i = 0; i < Math.Max(FEdgeFrom.SliceCount, FEdgeTo.SliceCount); i++)
                {
                    var edge = NewEdge(FEdgeFrom[i], FEdgeTo[i], i);
                    graph?.AddEdge(edge);
                }

                var cparams = new TLayoutParams();
                foreach (var prop in typeof(TLayoutParams).GetProperties())
                {
                    var input = pd.InputPins[prop.Name].Spread[0];
                    if (!(input.ToString().StartsWith("0") && input.ToString().EndsWith("0")))
                        prop.SetValue(cparams, pd.InputPins[prop.Name].Spread[0]);
                }
                layout = NewLayout(cparams);
                foreach (var prop in typeof(TLayoutParams).GetProperties())
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
                    FGraph[0] = graph;
                    FLayout[0] = layout;
                    GraphChanged = true;
                };
                if (FAsync[0])
                {
                    Task.Run(() => layout.Compute());
                }
                else
                {
                    layout.Compute();
                }
                init = true;
            }
            if (layout != null) FState[0] = layout.State.ToString();
            if (GraphChanged)
            {
                GraphChanged = false;
                FGraph.Stream.IsChanged = true;
                FLayout.Stream.IsChanged = true;
            }
        }
    }
}

