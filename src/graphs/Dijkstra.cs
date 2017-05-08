using System;
using System.Collections.Generic;
using QuickGraph;
using QuickGraph.Algorithms;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{
    [PluginInfo(
        Name = "Dijkstra",
        Category = "Graph",
        Version = "UniformCost",
        Tags = "Distance",
        Author = "microdee"
    )]
    public class GraphDrijkstraNode : IPluginEvaluate
    {
        [Input("Graph")]
        public ISpread<IVertexAndEdgeListGraph<Message, MessageEdge>> FGraph;
        [Input("From")]
        public ISpread<Message> FFrom;
        [Input("To")]
        public ISpread<Message> FTo;
        [Input("Compute", IsBang = true)]
        public ISpread<bool> FComp;
        
        [Output("Path")]
        public ISpread<ISpread<MessageEdge>> FPath;
        
        public void Evaluate(int SpreadMax)
        {
            if (FComp[0])
            {
                FPath.SliceCount = Math.Max(FFrom.SliceCount, FTo.SliceCount);
                for (int i = 0; i < FPath.SliceCount; i++)
                {
                    var tryGetPaths = FGraph[0].ShortestPathsDijkstra(edge => 1, FFrom[i]);
                    IEnumerable<MessageEdge> path;
                    FPath[i].SliceCount = 0;
                    if (tryGetPaths(FTo[i], out path))
                    {
                        foreach (var edge in path)
                        {
                            FPath[i].Add(edge);
                        }
                    }
                }
            }
        }
    }
}
