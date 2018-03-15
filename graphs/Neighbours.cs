using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphSharp;
using QuickGraph;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Graphs
{
    [PluginInfo(
        Name = "Neighbours",
        Category = "Graph",
        Version = "OutEdge",
        Author = "microdee"
    )]
    public class GraphOutEdgeNeighboursNode : IPluginEvaluate
    {
        [Input("Graph")]
        public ISpread<IVertexAndEdgeListGraph<Message, MessageEdge>> FGraph;
        [Input("Reference")]
        public ISpread<Message> FRef;
        [Input("Compute", IsBang = true)]
        public ISpread<bool> FComp;

        [Output("Neighbours")]
        public ISpread<ISpread<MessageEdge>> FNeighbours;
        [Output("Running")]
        public ISpread<bool> FRunning;
        [Output("Done", IsBang = true)]
        public ISpread<bool> FDone;

        private Task Eval;
        Spread<List<MessageEdge>> FoundEdges = new Spread<List<MessageEdge>>();

        public void Evaluate(int SpreadMax)
        {
            FDone[0] = false;
            if (FComp[0])
            {
                Eval = new Task(() =>
                {
                    FoundEdges.SliceCount = FRef.SliceCount;
                    for (int i = 0; i < FoundEdges.SliceCount; i++)
                    {
                        FoundEdges[i] = new List<MessageEdge>();
                        foreach (var edge in FGraph[0].OutEdges(FRef[i]))
                        {
                            FoundEdges[i].Add(edge);
                        }
                    }
                });
                Eval.Start();
                FRunning[0] = true;
            }
            if (Eval != null && Eval.IsCompleted && FRunning[0])
            {
                FNeighbours.SliceCount = FoundEdges.SliceCount;
                for (int i = 0; i < FNeighbours.SliceCount; i++)
                {
                    FNeighbours[i].SliceCount = FoundEdges[i].Count;
                    for (int j = 0; j < FoundEdges[i].Count; j++)
                    {
                        FNeighbours[i][j] = FoundEdges[i][j];
                    }
                }
                FDone[0] = true;
                FRunning[0] = false;
            }
        }
    }

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
        [Input("Levels", DefaultValue = 1, MinValue = 1)]
        public ISpread<int> FLevels;

        [Output("Neighbours")]
        public ISpread<ISpread<MessageEdge>> FNeighbours;
        [Output("Neighbour Edge Level")]
        public ISpread<ISpread<int>> FNeighbourLevels;
        [Output("Neighbour Nodes")]
        public ISpread<ISpread<Message>> FNeighbourNodes;
        [Output("Neighbour Node Level")]
        public ISpread<ISpread<int>> FNeighbourNodeLevels;
        [Output("Running")]
        public ISpread<bool> FRunning;
        [Output("Done", IsBang = true)]
        public ISpread<bool> FDone;

        private Spread<Task> Eval = new Spread<Task>();
        Spread<List<Tuple<MessageEdge, int>>> FoundEdges = new Spread<List<Tuple<MessageEdge, int>>>();
        Spread<List<Tuple<Message, int>>> FoundNodes = new Spread<List<Tuple<Message, int>>>();
        Spread<List<Message>> RefNodes = new Spread<List<Message>>();

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < FDone.SliceCount; i++)
            {
                FDone[i] = false;
            }
            FRunning.SliceCount = FRef.SliceCount;
            FDone.SliceCount = FRef.SliceCount;
            Eval.SliceCount = FRef.SliceCount;
            FoundEdges.SliceCount = FRef.SliceCount;
            FoundNodes.SliceCount = FRef.SliceCount;
            RefNodes.SliceCount = FRef.SliceCount;
            for (int i = 0; i < FRef.SliceCount; i++)
            {
                if (FComp[i])
                {
                    Eval[i] = new Task(state =>
                    {
                        var ii = (int)state;
                        FoundEdges[ii] = new List<Tuple<MessageEdge, int>>();
                        FoundNodes[ii] = new List<Tuple<Message, int>>();
                        RefNodes[ii] = new List<Message> {FRef[ii]};
                        for (int l = 0; l < FLevels[ii]; l++)
                        {
                            var stagingrefs = new List<Message>();
                            foreach (var rnode in RefNodes[ii])
                            {
                                foreach (var edge in FGraph[0].Edges)
                                {
                                    if (!edge.Source.Equals(rnode) && !edge.Target.Equals(rnode)) continue;
                                    if (FoundEdges[ii].Any(tuple => tuple.Item1.Equals(edge))) continue;
                                    FoundEdges[ii].Add(new Tuple<MessageEdge, int>(edge, l));
                                    if (!edge.Source.Equals(rnode) && !FoundNodes[ii].Any(tuple => tuple.Item1.Equals(edge.Source)))
                                    {
                                        FoundNodes[ii].Add(new Tuple<Message, int>(edge.Source, l));
                                        stagingrefs.Add(edge.Source);
                                    }
                                    if (!edge.Target.Equals(rnode) && !FoundNodes[ii].Any(tuple => tuple.Item1.Equals(edge.Target)))
                                    {
                                        FoundNodes[ii].Add(new Tuple<Message, int>(edge.Target, l));
                                        stagingrefs.Add(edge.Target);
                                    }
                                }
                            }
                            RefNodes[ii] = stagingrefs;
                        }
                    }, i);
                    Eval[i].Start();
                    FRunning[i] = true;
                }
            }
            FNeighbours.SliceCount = FoundEdges.SliceCount;
            FNeighbourLevels.SliceCount = FoundEdges.SliceCount;
            FNeighbourNodes.SliceCount = FoundNodes.SliceCount;
            FNeighbourNodeLevels.SliceCount = FoundNodes.SliceCount;
            for (int i = 0; i < Eval.SliceCount; i++)
            {
                if (Eval[i] != null && Eval[i].IsCompleted && FRunning[i])
                {
                    FNeighbours[i].SliceCount = FoundEdges[i].Count;
                    FNeighbourLevels[i].SliceCount = FoundEdges[i].Count;
                    FNeighbourNodes[i].SliceCount = FoundNodes[i].Count;
                    FNeighbourNodeLevels[i].SliceCount = FoundNodes[i].Count;
                    for (int j = 0; j < FoundEdges[i].Count; j++)
                    {
                        FNeighbours[i][j] = FoundEdges[i][j].Item1;
                        FNeighbourLevels[i][j] = FoundEdges[i][j].Item2;
                    }
                    for (int j = 0; j < FoundNodes[i].Count; j++)
                    {
                        FNeighbourNodes[i][j] = FoundNodes[i][j].Item1;
                        FNeighbourNodeLevels[i][j] = FoundNodes[i][j].Item2;
                    }
                    FDone[i] = true;
                    FRunning[i] = false;
                }
                else
                {
                    FNeighbours[i].SliceCount = 0;
                }
            }
        }
    }
}
