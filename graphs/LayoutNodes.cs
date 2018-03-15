using System.Collections.Generic;
using System.Windows;
using GraphSharp;
using GraphSharp.Algorithms.Layout.Compound;
using GraphSharp.Algorithms.Layout.Compound.FDP;
using GraphSharp.Algorithms.Layout.Simple.FDP;
using QuickGraph;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Graphs
{
    [PluginInfo(
        Name = "ISOM",
        Category = "GraphLayout",
        Author = "microdee"
    )]
    public class IsomGraphLayoutNode : AbstractGraphLayoutGeneratorNode<
        Message,
        MessageEdge,
        BidirectionalGraph<Message, MessageEdge>,
        ISOMLayoutAlgorithm<Message, MessageEdge, BidirectionalGraph<Message, MessageEdge>>,
        ISOMLayoutParameters>
    {
        public override BidirectionalGraph<Message, MessageEdge> NewGraph()
        {
            return new BidirectionalGraph<Message, MessageEdge>(true);
        }

        public override ISOMLayoutAlgorithm<Message, MessageEdge, BidirectionalGraph<Message, MessageEdge>> NewLayout(ISOMLayoutParameters cparams)
        {
            return new ISOMLayoutAlgorithm<Message, MessageEdge, BidirectionalGraph<Message, MessageEdge>>(graph, VPos, cparams);
        }

        public override MessageEdge NewEdge(Message from, Message to, int i)
        {
            var res = new MessageEdge(from, to)
            {
                ID = i
            };
            return res;
        }
    }

    [PluginInfo(
        Name = "CompoundFDP",
        Category = "GraphLayout",
        Author = "microdee"
    )]
    public class CompoundFdpGraphLayoutNode : AbstractGraphLayoutGeneratorNode<
        Message,
        MessageEdge,
        CompoundGraph<Message, MessageEdge>,
        CompoundFDPLayoutAlgorithm<Message, MessageEdge, CompoundGraph<Message, MessageEdge>>,
        CompoundFDPLayoutParameters>
    {

        [Input("Vertex Type", DefaultEnumEntry = "Automatic")]
        public ISpread<CompoundVertexInnerLayoutType> FVType;

        private Dictionary<Message, CompoundVertexInnerLayoutType> VType = new Dictionary<Message, CompoundVertexInnerLayoutType>();

        public override void InitializeBuild()
        {
            VType.Clear();

            for (int i = 0; i < FVertices.SliceCount; i++)
            {
                VType.Add(FVertices[i], FVType[i]);
            }
        }

        public override CompoundGraph<Message, MessageEdge> NewGraph()
        {
            return new CompoundGraph<Message, MessageEdge>(true);
        }

        public override CompoundFDPLayoutAlgorithm<Message, MessageEdge, CompoundGraph<Message, MessageEdge>> NewLayout(CompoundFDPLayoutParameters cparams)
        {
            return new CompoundFDPLayoutAlgorithm<Message, MessageEdge, CompoundGraph<Message, MessageEdge>>(graph, VSize, VThickness, VType, VPos, cparams);
        }

        public override MessageEdge NewEdge(Message from, Message to, int i)
        {
            return new MessageEdge(from, to)
            {
                ID = i
            };
        }
    }
}
