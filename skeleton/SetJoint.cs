using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.SkeletonV2
{
    public enum SetJointOperationMode
    {
        Absolute,
        Override,
        FirstOperand,
        SecondOperand
    }

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
        public ISpread<ISpread<Matrix4x4>> FAnimTrIn;
        [Input("Joint Name")]
        public ISpread<ISpread<string>> FJointName;
        [Input("Regex Search", Visibility = PinVisibility.Hidden)]
        public IDiffSpread<bool> FRegexSearch;
        [Input("Operation Mode", Visibility = PinVisibility.Hidden, DefaultEnumEntry = "Override")]
        public IDiffSpread<SetJointOperationMode> FOpMode;

        [Output("Skeleton Out")]
        public ISpread<ISkeleton> FOut;
        [Output("Joint Out")]
        public ISpread<ISpread<IJoint>> FJoint;
        [Output("Name Index", Visibility = PinVisibility.Hidden)]
        public ISpread<ISpread<int>> FNameIndex;
        [Output("Success")]
        public ISpread<ISpread<bool>> FSucc;

        private bool init = true;

        public void Evaluate(int SpreadMax)
        {
            if (FSkeletonIn.SliceCount == 0)
            {
                FOut.SliceCount = 0;
                FSucc.SliceCount = 0;
                FJoint.SliceCount = 0;
                FNameIndex.SliceCount = 0;
                return;
            }
            if (FSkeletonIn[0] == null) return;

            if (FSkeletonIn.IsChanged ||
                FJointName.IsChanged ||
                FAnimTrIn.Stream.IsChanged ||
                FBaseTrIn.Stream.IsChanged ||
                FRegexSearch.IsChanged ||
                FOpMode.IsChanged)
            {
                var spreadmax = new[] { FAnimTrIn.SliceCount, FBaseTrIn.SliceCount, FJointName.SliceCount, FSkeletonIn.SliceCount }.Max();
                FOut.SliceCount = spreadmax;
                FSucc.SliceCount = spreadmax;
                FJoint.SliceCount = spreadmax;
                FNameIndex.SliceCount = spreadmax;
                FJoint.Stream.IsChanged = true;
                FOut.Stream.IsChanged = true;
                for (int i = 0; i < FOut.SliceCount; i++)
                {
                    if (FOut[i] == null) FOut[i] = new Skeleton();
                    FSkeletonIn[i].CopyData(FOut[i]);
                    FSucc[i].SliceCount = FJointName[i].SliceCount;

                    if (FJointName.IsChanged || FRegexSearch.IsChanged || init)
                    {
                        FNameIndex[i].SliceCount = 0;
                        if (FRegexSearch[i])
                        {
                            FJoint[i].SliceCount = 0;
                            for (int j = 0; j < FJointName[i].SliceCount; j++)
                            {
                                var pattern = new Regex(FJointName[i][j], RegexOptions.CultureInvariant | RegexOptions.Multiline);
                                FSucc[i][j] = false;
                                foreach (var joint in FOut[i].JointTable.Values)
                                {
                                    if(!pattern.IsMatch(joint.Name)) continue;
                                    FJoint[i].Add(joint);
                                    FNameIndex[i].Add(j);
                                    FSucc[i][j] = true;
                                }
                            }
                        }
                        else
                        {
                            FJoint[i].SliceCount = FJointName[i].SliceCount;
                            for (int j = 0; j < FJointName[i].SliceCount; j++)
                            {
                                if (!FOut[i].JointTable.ContainsKey(FJointName[i][j]))
                                {
                                    FSucc[i][j] = false;
                                    continue;
                                }
                                FSucc[i][j] = true;
                                var joint = FOut[i].JointTable[FJointName[i][j]];
                                FJoint[i][j] = joint;
                            }
                        }
                    }
                    for (int j = 0; j < FJoint[i].SliceCount; j++)
                    {
                        var joint = FJoint[i][j];
                        var jointin = FSkeletonIn[i].JointTable[FJoint[i][j].Name];
                        int jj = FRegexSearch[i] ? FNameIndex[i][j] : j;
                        switch (FOpMode[i])
                        {
                            case SetJointOperationMode.Absolute:
                                if(joint.Parent != null)
                                {
                                    joint.BaseTransform = FBaseTrIn[i][jj] * VMath.Inverse(joint.Parent.CombinedTransform);
                                }
                                else joint.BaseTransform = FBaseTrIn[i][jj];

                                joint.AnimationTransform = FAnimTrIn[i][jj];
                                break;
                            case SetJointOperationMode.Override:
                                joint.BaseTransform = FBaseTrIn[i][jj];
                                joint.AnimationTransform = FAnimTrIn[i][jj];
                                break;
                            case SetJointOperationMode.FirstOperand:
                                joint.BaseTransform = FBaseTrIn[i][jj] * jointin.BaseTransform;
                                joint.AnimationTransform = FAnimTrIn[i][jj] * jointin.AnimationTransform;
                                break;
                            case SetJointOperationMode.SecondOperand:
                                joint.BaseTransform = jointin.BaseTransform * FBaseTrIn[i][jj];
                                joint.AnimationTransform = jointin.AnimationTransform * FAnimTrIn[i][jj];
                                break;
                        }
                    }
                }
                init = false;
            }
            else
            {
                FJoint.Stream.IsChanged = false;
            }
        }
    }
}
