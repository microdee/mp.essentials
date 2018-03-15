using System;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.Generic;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.Streams;

namespace mp.essentials.Nodes.SkeletonV2
{
    [PluginInfo(Name = "FrameDelay",
                Category = "Skeleton.V2",
                Help = "Delays the input value one calculation frame.",
                Tags = "generic"
               )]
    public class ISkeletonFrameDelayNode : FrameDelayNode<ISkeleton>
    {
        public ISkeletonFrameDelayNode() : base(ISkeletonCopier.Default) { }
    }

    class ISkeletonCopier : Copier<ISkeleton>
    {
        public static readonly ISkeletonCopier Default = new ISkeletonCopier();

        public override ISkeleton Copy(ISkeleton value)
        {
            return value.DeepCopy();
        }
    }

    #region SpreadOps

    [PluginInfo(Name = "Cons",
                Category = "Skeleton.V2",
                Help = "Concatenates all Input spreads.",
                Tags = "generic, spreadop"
                )]
    public class ISkeletonConsNode : Cons<ISkeleton> { }

    [PluginInfo(Name = "CAR",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Splits a given spread into first slice and remainder.",
                Tags = "split, generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonCARBinNode : CARBin<ISkeleton> { }

    [PluginInfo(Name = "CDR",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Splits a given spread into remainder and last slice.",
                Tags = "split, generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonCDRBinNode : CDRBin<ISkeleton> { }

    [PluginInfo(Name = "Reverse",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Reverses the order of the slices in the Spread.",
                Tags = "invert, generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonReverseBinNode : ReverseBin<ISkeleton> { }

    [PluginInfo(Name = "Shift",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Shifts the slices in the Spread by the given Phase.",
                Tags = "generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonShiftBinNode : ShiftBin<ISkeleton> { }

    [PluginInfo(Name = "SetSlice",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Replaces individual slices of a spread with the given input",
                Tags = "generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonSetSliceNode : SetSlice<ISkeleton> { }

    [PluginInfo(Name = "DeleteSlice",
                Category = "Skeleton.V2",
                Help = "Removes slices from the Spread at the positions addressed by the Index pin.",
                Tags = "remove, generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonDeleteSliceNode : DeleteSlice<ISkeleton> { }

    [PluginInfo(Name = "Select",
                Category = "Skeleton.V2",
                Help = "Returns each slice of the Input spread as often as specified by the corresponding Select slice. 0 meaning the slice will be omitted. ",
                Tags = "repeat, resample, duplicate, spreadop"
               )]
    public class ISkeletonSelectNode : Select<ISkeleton> { }

    [PluginInfo(Name = "Select",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Returns each slice of the Input spread as often as specified by the corresponding Select slice. 0 meaning the slice will be omitted. ",
                Tags = "repeat, resample, duplicate, spreadop",
                Author = "woei"
            )]
    public class ISkeletonSelectBinNode : SelectBin<ISkeleton> { }

    [PluginInfo(Name = "Unzip",
                Category = "Skeleton.V2",
                Help = "The inverse of Zip. Interprets the Input spread as being interleaved and untangles it.",
                Tags = "split, generic, spreadop"
               )]
    public class ISkeletonUnzipNode : Unzip<ISkeleton> { }

    [PluginInfo(Name = "Unzip",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "The inverse of Zip. Interprets the Input spread as being interleaved and untangles it. With Bin Size.",
                Tags = "split, generic, spreadop"
               )]
    public class ISkeletonUnzipBinNode : Unzip<IInStream<ISkeleton>> { }

    [PluginInfo(Name = "Zip",
                Category = "Skeleton.V2",
                Help = "Interleaves all Input spreads.",
                Tags = "interleave, join, generic, spreadop"
               )]
    public class REPLACEME_CLASSZipNode : Zip<ISkeleton> { }

    [PluginInfo(Name = "Zip",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Interleaves all Input spreads.",
                Tags = "interleave, join, generic, spreadop"
               )]
    public class REPLACEME_CLASSZipBinNode : Zip<IInStream<ISkeleton>> { }

    [PluginInfo(Name = "GetSpread",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Returns sub-spreads from the input specified via offset and count",
                Tags = "generic, spreadop",
                Author = "woei")]
    public class ISkeletonGetSpreadNode : GetSpreadAdvanced<ISkeleton> { }

    [PluginInfo(Name = "SetSpread",
                Category = "Skeleton.V2",
                Version = "Bin",
                Help = "Allows to set sub-spreads into a given spread.",
                Tags = "generic, spreadop",
                Author = "woei"
               )]
    public class ISkeletonSetSpreadNode : SetSpread<ISkeleton> { }

    [PluginInfo(Name = "Pairwise",
                Category = "Skeleton.V2",
                Help = "Returns all combinations of pairs of successive slices. From an input ABCD returns AB, BC, CD.",
                Tags = "generic, spreadop"
                )]
    public class ISkeletonPairwiseNode : Pairwise<ISkeleton> { }

    [PluginInfo(Name = "SplitAt",
                Category = "Skeleton.V2",
                Help = "Splits the Input spread in two at the specified Index.",
                Tags = "generic, spreadop"
                )]
    public class ISkeletonSplitAtNode : SplitAtNode<ISkeleton> { }

    #endregion SpreadOps

    #region Collections

    [PluginInfo(Name = "Buffer",
                Category = "Skeleton.V2",
                Help = "Inserts the input at the given index.",
                Tags = "generic, spreadop, collection",
                AutoEvaluate = true
               )]
    public class ISkeletonBufferNode : BufferNode<ISkeleton>
    {
        public ISkeletonBufferNode() : base(ISkeletonCopier.Default) { }
    }

    [PluginInfo(Name = "Queue",
                Category = "Skeleton.V2",
                Help = "Inserts the input at index 0 and drops the oldest slice in a FIFO (First In First Out) fashion.",
                Tags = "generic, spreadop, collection",
                AutoEvaluate = true
               )]
    public class ISkeletonQueueNode : QueueNode<ISkeleton>
    {
        public ISkeletonQueueNode() : base(ISkeletonCopier.Default) { }
    }

    [PluginInfo(Name = "RingBuffer",
                Category = "Skeleton.V2",
                Help = "Inserts the input at the ringbuffer position.",
                Tags = "generic, spreadop, collection",
                AutoEvaluate = true
               )]
    public class ISkeletonRingBufferNode : RingBufferNode<ISkeleton>
    {
        public ISkeletonRingBufferNode() : base(ISkeletonCopier.Default) { }
    }

    [PluginInfo(Name = "Store",
                Category = "Skeleton.V2",
                Help = "Stores a spread and sets/removes/inserts slices.",
                Tags = "add, insert, remove, generic, spreadop, collection",
                Author = "woei",
                AutoEvaluate = true
               )]
    public class ISkeletonStoreNode : Store<ISkeleton> { }

    [PluginInfo(Name = "Stack",
                Category = "Skeleton.V2",
                Help = "Stack data structure implementation using the LIFO (Last In First Out) paradigm.",
                Tags = "generic, spreadop, collection",
                Author = "vux"
                )]
    public class ISkeletonStackNode : StackNode<ISkeleton> { }

    [PluginInfo(
           Name = "QueueStore",
           Category = "Skeleton.V2",
           Help = "Stores a series of queues",
           Tags = "append, remove, generic, spreadop, collection",
           Author = "motzi"
    )]
    public class ISkeletonQueueStoreNodes : QueueStore<ISkeleton>
    {
        ISkeletonQueueStoreNodes() : base(ISkeletonCopier.Default) { }
    }

    #endregion Collections
}