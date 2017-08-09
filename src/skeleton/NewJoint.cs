using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Nodes.Generic;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.Streams;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.SkeletonV2
{
    [PluginInfo(
        Name = "Joint",
        Category = "Skeleton",
        Version = "V2",
        Author = "microdee"
    )]
    public class V2SkeletonNewJoint : IPluginEvaluate
    {
        [Input("Joint Name")]
        public IDiffSpread<string> FJointNameIn;
        [Input("Base Transform")]
        public IDiffSpread<Matrix4x4> FBaseTrIn;
        [Input("Constraints ", DimensionNames = new[] { "Min", "Max" }, DefaultValues = new [] {-1.0, 1.0}, BinSize = 3)]
        public IDiffSpread<ISpread<Vector2D>> FConstraintsIn;
        [Input("Children")]
        public IDiffSpread<ISpread<IJoint>> FChildren;

        [Output("Joint")]
        public ISpread<JointInfoV2> FOut;

        public void Evaluate(int SpreadMax)
        {
            if (FJointNameIn.SliceCount == 0 || FConstraintsIn.SliceCount == 0 || FBaseTrIn.SliceCount == 0)
            {
                FOut.SliceCount = 0;
                return;
            }
            if (FJointNameIn.IsChanged || FConstraintsIn.IsChanged || FBaseTrIn.IsChanged || FChildren.IsChanged)
            {
                FOut.Stream.IsChanged = true;
                var spreadmax = new[] { FJointNameIn.SliceCount, FBaseTrIn.SliceCount, FConstraintsIn.SliceCount, FChildren.SliceCount };
                for (int i = 0; i < spreadmax.Max(); i++)
                {
                    if (FOut[i] == null) FOut[i] = new JointInfoV2(0, FJointNameIn[i]);
                    FOut[i].Name = FJointNameIn[i];
                    FOut[i].BaseTransform = new Matrix4x4(FBaseTrIn[i]);
                    FOut[i].Constraints = FConstraintsIn[i].ToList();

                    if(FChildren.SliceCount == 0) continue;
                    if(FChildren[i][0] == null) continue;

                    FOut[i].ChildrenChanged = false;
                    for (int j = 0; j < FChildren[i].SliceCount; j++)
                    {
                        if (!(FChildren[i][j] is JointInfoV2)) continue;
                        var joint = (JointInfoV2)FChildren[i][j];
                        if (joint.ChildrenChanged)
                        {
                            FOut[i].ChildrenChanged = true;
                            break;
                        }
                    }
                    if (FOut[i].Children.Count == FChildren[i].SliceCount)
                    {
                        for (int j = 0; j < FOut[i].Children.Count; j++)
                        {
                            FOut[i].Children[j] = FChildren[i][j];
                        }
                    }
                    else if (FChildren[i].SliceCount == 0)
                    {
                        FOut[i].Children.Clear();
                        FOut[i].ChildrenChanged = true;
                    }
                    else
                    {
                        FOut[i].Children.Clear();
                        FOut[i].Children.AddRange(FChildren[i]);
                        FOut[i].ChildrenChanged = true;
                    }
                }
            }
            else
            {
                FOut.Stream.IsChanged = false;
            }
        }
    }

    [PluginInfo(
        Name = "Make",
        Category = "Skeleton",
        Version = "V2",
        Author = "microdee"
    )]
    public class V2SkeletonMakeNode : IPluginEvaluate
    {
        [Input("Joint")]
        public IDiffSpread<IJoint> FJoints;

        [Output("Skeleton")]
        public ISpread<Skeleton> FOut;

        public void Evaluate(int SpreadMax)
        {
            if (FJoints.SliceCount == 0)
            {
                FOut.SliceCount = 0;
                return;
            }
            if (FJoints.IsChanged)
            {
                FOut.Stream.IsChanged = true;
                for (int i = 0; i < FOut.SliceCount; i++)
                {
                    if (FOut[i] == null)
                    {
                        var newskeleton = new Skeleton(FJoints[i]);
                        newskeleton.RenewUid();
                        newskeleton.BuildJointTable();
                        var ii = 0;
                        foreach (var j in newskeleton.JointTable.Values)
                        {
                            j.Id = ii;
                            ii++;
                        }
                        FOut[i] = newskeleton;
                    }
                    if (!(FJoints[i] is JointInfoV2)) continue;
                    var joint = (JointInfoV2) FJoints[i];
                    if(joint.ChildrenChanged)
                        FOut[i].BuildJointTable();
                }
            }
            else
            {
                FOut.Stream.IsChanged = false;
            }
        }
    }

    [PluginInfo(Name = "Cons",
                Category = "Skeleton.Joint",
                Help = "Concatenates all Input spreads.",
                Tags = "generic, spreadop"
                )]
    public class IJointConsNode : Cons<IJoint> { }

    [PluginInfo(Name = "CAR",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Splits a given spread into first slice and remainder.",
                Tags = "split, generic, spreadop",
                Author = "woei"
               )]
    public class IJointCARBinNode : CARBin<IJoint> { }

    [PluginInfo(Name = "CDR",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Splits a given spread into remainder and last slice.",
                Tags = "split, generic, spreadop",
                Author = "woei"
               )]
    public class IJointCDRBinNode : CDRBin<IJoint> { }

    [PluginInfo(Name = "SetSlice",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Replaces individual slices of a spread with the given input",
                Tags = "generic, spreadop",
                Author = "woei"
               )]
    public class IJointSetSliceNode : SetSlice<IJoint> { }

    [PluginInfo(Name = "DeleteSlice",
                Category = "Skeleton.Joint",
                Help = "Removes slices from the Spread at the positions addressed by the Index pin.",
                Tags = "remove, generic, spreadop",
                Author = "woei"
               )]
    public class IJointDeleteSliceNode : DeleteSlice<IJoint> { }

    [PluginInfo(Name = "Select",
                Category = "Skeleton.Joint",
                Help = "Returns each slice of the Input spread as often as specified by the corresponding Select slice. 0 meaning the slice will be omitted. ",
                Tags = "repeat, resample, duplicate, spreadop"
               )]
    public class IJointSelectNode : Select<IJoint> { }

    [PluginInfo(Name = "Select",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Returns each slice of the Input spread as often as specified by the corresponding Select slice. 0 meaning the slice will be omitted. ",
                Tags = "repeat, resample, duplicate, spreadop",
                Author = "woei"
            )]
    public class IJointSelectBinNode : SelectBin<IJoint> { }

    [PluginInfo(Name = "Unzip",
                Category = "Skeleton.Joint",
                Help = "The inverse of Zip. Interprets the Input spread as being interleaved and untangles it.",
                Tags = "split, generic, spreadop"
               )]
    public class IJointUnzipNode : Unzip<IJoint> { }

    [PluginInfo(Name = "Unzip",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "The inverse of Zip. Interprets the Input spread as being interleaved and untangles it. With Bin Size.",
                Tags = "split, generic, spreadop"
               )]
    public class IJointUnzipBinNode : Unzip<IInStream<IJoint>> { }

    [PluginInfo(Name = "Zip",
                Category = "Skeleton.Joint",
                Help = "Interleaves all Input spreads.",
                Tags = "interleave, join, generic, spreadop"
               )]
    public class IJointZipNode : Zip<IJoint> { }

    [PluginInfo(Name = "Zip",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Interleaves all Input spreads.",
                Tags = "interleave, join, generic, spreadop"
               )]
    public class IJointZipBinNode : Zip<IInStream<IJoint>> { }

    [PluginInfo(Name = "GetSpread",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Returns sub-spreads from the input specified via offset and count",
                Tags = "generic, spreadop",
                Author = "woei")]
    public class IJointGetSpreadNode : GetSpreadAdvanced<IJoint> { }

    [PluginInfo(Name = "SetSpread",
                Category = "Skeleton.Joint",
                Version = "Bin",
                Help = "Allows to set sub-spreads into a given spread.",
                Tags = "generic, spreadop",
                Author = "woei"
               )]
    public class IJointSetSpreadNode : SetSpread<IJoint> { }

    [PluginInfo(Name = "SplitAt",
                Category = "Skeleton.Joint",
                Help = "Splits the Input spread in two at the specified Index.",
                Tags = "generic, spreadop"
                )]
    public class IJointSplitAtNode : SplitAtNode<IJoint> { }
}
