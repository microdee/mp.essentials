using System.Threading.Tasks;
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Graphs
{
    [PluginInfo(
        Name = "DistanceFromReference",
        Category = "Graph",
        Author = "microdee"
    )]
    public class GraphDistanceFromReferenceNode : IPluginEvaluate
    {
        [Input("Graph")]
        public ISpread<IVertexAndEdgeListGraph<Message, MessageEdge>> FGraph;
        [Input("Reference")]
        public ISpread<Message> FRef;
        [Input("Vertices")]
        public ISpread<Message> FVerts;
        [Input("Async")]
        public ISpread<bool> FAsync;
        [Input("Compute", IsBang = true)]
        public ISpread<bool> FComp;

        [Output("Distance")]
        public ISpread<double> FDist;
        [Output("Success")]
        public ISpread<bool> FSucc;
        [Output("Status")]
        public ISpread<string> FState;

        private FloydWarshallAllShortestPathAlgorithm<Message, MessageEdge> alg;

        public void Evaluate(int SpreadMax)
        {
            if (FComp[0])
            {
                alg = new FloydWarshallAllShortestPathAlgorithm<Message, MessageEdge>(FGraph[0], edge => 1);
                //alg.SetRootVertex(FRef[0]);
                alg.Finished += (sender, args) =>
                {
                    FDist.SliceCount = FVerts.SliceCount;
                    FSucc.SliceCount = FVerts.SliceCount;
                    for (int i = 0; i < FVerts.SliceCount; i++)
                    {
                        double result = -1;
                        FSucc[i] = alg.TryGetDistance(FRef[0], FVerts[i], out result);
                        FDist[i] = result;
                    }
                };
                if (FAsync[0])
                {
                    Task.Run(() => alg.Compute());
                }
                else
                {
                    alg.Compute();
                }
            }
            FState[0] = alg?.State.ToString();
        }
    }
}
