using GraphSharp;
using QuickGraph;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{
    [PluginInfo(
        Name = "Neighbours",
        Category = "Graph",
        Author = "microdee"
    )]
    public class GraphNeighboursNode : IPluginEvaluate
    {
        [Input("Graph")]
        public ISpread<IVertexAndEdgeListGraph<Message, MessageEdge>> FGraph;
        [Input("Reference")]
        public ISpread<Message> FRef;
        [Input("Compute", IsBang = true)]
        public ISpread<bool> FComp;
        
        [Output("Neighbours")]
        public ISpread<ISpread<MessageEdge>> FNeighbours;
        
        public void Evaluate(int SpreadMax)
        {
            if (FComp[0])
            {
                FNeighbours.SliceCount = FRef.SliceCount;
                for (int i = 0; i < FNeighbours.SliceCount; i++)
                {
                    var neighbours = FGraph[0].OutEdges(FRef[i]);
                    FNeighbours[i].SliceCount = 0;
                    foreach (var v in neighbours)
                    {
                        FNeighbours[i].Add(v);
                    }
                }
            }
        }
    }
}
