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
         Name = "GetJointTransform",
         Category = "Skeleton",
         Version = "V2",
         Author = "microdee"
     )]
    public class V2SkeletonGetJointTransform : IPluginEvaluate
    {
        [Input("Skeleton")]
        public IDiffSpread<ISkeleton> FSkeletonIn;
        [Input("Inverse Bind Pose")]
        public ISpread<ISpread<Matrix4x4>> FInvBindTr;
        [Input("Joint Name")]
        public ISpread<ISpread<string>> FJointName;

        [Output("Transform Out")]
        public ISpread<ISpread<Matrix4x4>> FOut;
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
            if (FSkeletonIn.IsChanged || FJointName.IsChanged || FInvBindTr.IsChanged)
            {
                FOut.SliceCount = FSkeletonIn.SliceCount;
                FSucc.SliceCount = FSkeletonIn.SliceCount;
                FJoint.SliceCount = FSkeletonIn.SliceCount;
                FJoint.Stream.IsChanged = true;
                for (int i = 0; i < FOut.SliceCount; i++)
                {
                    var getall = false;
                    var skspreadmax = Math.Max(FInvBindTr[i].SliceCount, FJointName[i].SliceCount);
                    if (FJointName[i].SliceCount > 0 && string.IsNullOrWhiteSpace(FJointName[i][0]))
                    {
                        skspreadmax = FSkeletonIn[i].JointTable.Count;
                        getall = true;
                    }
                    FOut[i].SliceCount = skspreadmax;
                    FSucc[i].SliceCount = skspreadmax;
                    FJoint[i].SliceCount = skspreadmax;
                    if (getall)
                    {
                        var joints = FSkeletonIn[i].JointTable.Values.ToArray();
                        for (int j = 0; j < skspreadmax; j++)
                        {
                            try
                            {
                                FOut[i][j] = FInvBindTr[i][j] * joints[j].CombinedTransform;
                                FJoint[i][j] = joints[j];
                                FSucc[i][j] = true;
                            }
                            catch
                            {
                                FSucc[i][j] = false;
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < skspreadmax; j++)
                        {
                            try
                            {
                                if (FSkeletonIn[i].JointTable.ContainsKey(FJointName[i][j]))
                                {
                                    var currjoint = FSkeletonIn[i].JointTable[FJointName[i][j]];
                                    FOut[i][j] = FInvBindTr[i][j] * currjoint.CombinedTransform;
                                    FJoint[i][j] = currjoint;
                                    FSucc[i][j] = true;
                                }
                                else
                                {
                                    FSucc[i][j] = false;
                                }
                            }
                            catch
                            {
                                FSucc[i][j] = false;
                            }
                        }
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
