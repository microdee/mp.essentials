using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.SkeletonV2
{
    [PluginInfo(
         Name = "SetJoint",
         Category = "Skeleton",
         Version = "V2",
         Author = "microdee"
     )]
    public class V2SkeletonSetJoint : IPluginEvaluate
    {
        [Input("Skeleton")]
        public Pin<ISkeleton> FSkeletonIn;
        [Input("Base Transform")]
        public IDiffSpread<ISpread<Matrix4x4>> FBaseTrIn;
        [Input("Animation Transform")]
        public IDiffSpread<ISpread<Matrix4x4>> FAnimTrIn;
        [Input("Joint Name")]
        public IDiffSpread<ISpread<string>> FJointName;

        [Output("Skeleton Out")]
        public ISpread<ISkeleton> FOut;
        [Output("Joint Out")]
        public ISpread<ISpread<IJoint>> FJoint;
        [Output("Success")]
        public ISpread<ISpread<bool>> FSucc;

        public void Evaluate(int SpreadMax)
        {
            if (FSkeletonIn.SliceCount == 0)
            {
                FOut.SliceCount = 0;
                FSucc.SliceCount = 0;
                FJoint.SliceCount = 0;
                return;
            }
            if (FSkeletonIn[0] == null) return;

            if (FSkeletonIn.IsChanged ||
                FJointName.IsChanged ||
                FAnimTrIn.IsChanged ||
                FBaseTrIn.IsChanged)
            {
                var spreadmax = new[] { FAnimTrIn.SliceCount, FBaseTrIn.SliceCount, FJointName.SliceCount, FSkeletonIn.SliceCount }.Max();
                FOut.SliceCount = spreadmax;
                FSucc.SliceCount = spreadmax;
                FJoint.SliceCount = spreadmax;
                FJoint.Stream.IsChanged = true;
                FOut.Stream.IsChanged = true;
                for (int i = 0; i < FOut.SliceCount; i++)
                {
                    FSucc[i].SliceCount = FJointName[i].SliceCount;
                    FJoint[i].SliceCount = FJointName[i].SliceCount;
                    if (FOut[i] == null) FOut[i] = new Skeleton();
                    else FSkeletonIn[i].CopyData(FOut[i]);
                    for (int j = 0; j < FJointName[i].SliceCount; j++)
                    {
                        if (!FOut[i].JointTable.ContainsKey(FJointName[i][j]))
                        {
                            FSucc[i][j] = false;
                            continue;
                        }
                        var joint = FOut[i].JointTable[FJointName[i][j]];
                        FJoint[i][j] = joint;
                        if(FBaseTrIn.IsChanged) joint.BaseTransform = FBaseTrIn[i][j];
                        if(FAnimTrIn.IsChanged) joint.AnimationTransform = FAnimTrIn[i][j];
                        FSucc[i][j] = true;
                    }
                }
            }
            else
            {
                FJoint.Stream.IsChanged = false;
            }
        }
    }
}
