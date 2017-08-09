using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.SkeletonV2
{
    [PluginInfo(
        Name = "CreateJoint",
        Category = "Skeleton",
        Version = "V2",
        Author = "microdee"
    )]
    public class V2SkeletonCreateJoint : IPluginEvaluate
    {
        public enum OffsetMode
        {
            Parent,
            World
        }

        [Input("Joint Name")]
        public IDiffSpread<ISpread<string>> FJointNameIn;
        [Input("Parent Name")]
        public IDiffSpread<ISpread<string>> FParentNameIn;
        [Input("Base Transform")]
        public IDiffSpread<ISpread<Matrix4x4>> FBaseTrIn;
        [Input("Constraints ", DimensionNames = new []{"Min", "Max"}, DefaultValues = new[] { -1.0, 1.0 })]
        public IDiffSpread<ISpread<Vector2D>> FConstraintsIn;
        [Input("Position Relative to")]
        public IDiffSpread<OffsetMode> FOffsetModeIn;

        [Output("Skeleton")]
        public ISpread<Skeleton> FOut;

        public void Evaluate(int SpreadMax)
        {
            if (
                FJointNameIn.SliceCount == 0 ||
                FParentNameIn.SliceCount == 0 ||
                FBaseTrIn.SliceCount == 0 ||
                FConstraintsIn.SliceCount == 0 ||
                FOffsetModeIn.SliceCount == 0
                )
            {
                FOut.SliceCount = 0;
                return;
            }
            if (
                FJointNameIn.IsChanged ||
                FParentNameIn.IsChanged ||
                FBaseTrIn.IsChanged ||
                FConstraintsIn.IsChanged ||
                FOffsetModeIn.IsChanged
            )
            {
                FOut.Stream.IsChanged = true;
                var spreadmax = new [] {FJointNameIn.SliceCount, FParentNameIn.SliceCount, FBaseTrIn.SliceCount, FConstraintsIn.SliceCount};
                for (int i = 0; i < spreadmax.Max(); i++)
                {
                    if (FOut[i] == null) FOut[i] = new Skeleton();
                    FOut[i].RenewUid();
                    var currid = 0;
                    for (int j = 0; j < FJointNameIn[i].SliceCount; j++)
                    {
                        if(string.IsNullOrWhiteSpace(FJointNameIn[i][j])) continue;
                        var currjoint = new JointInfoV2(0, FJointNameIn[i][j]);
                        currjoint.BaseTransform = FBaseTrIn[i][j];
                        currjoint.Constraints.Clear();
                        for (int k = j*3; k < j*3 + 3; k++)
                        {
                            currjoint.Constraints.Add(new Vector2D(FConstraintsIn[i][k]));
                        }
                        if (string.IsNullOrWhiteSpace(FParentNameIn[i][j]))
                        {
                            if (FOut[i].Root == null)
                            {
                                FOut[i].Root = currjoint;
                                currjoint.Id = currid;
                                currid++;
                                FOut[i].BuildJointTable();
                            }
                        }
                        else
                        {
                            if (FOut[i].JointTable.ContainsKey(FParentNameIn[i][j]))
                            {
                                currjoint.Parent = FOut[i].JointTable[FParentNameIn[i][j]];
                                currjoint.Id = currid;
                                currid++;
                                FOut[i].BuildJointTable();
                            }
                        }

                        if (FOffsetModeIn[i] == OffsetMode.World)
                        {
                            List<Vector3D> offsetList = new List<Vector3D>();
                            foreach (KeyValuePair<string, IJoint> pair in FOut[i].JointTable)
                            {
                                Vector3D worldPos = pair.Value.BaseTransform * (new Vector3D(0));
                                Vector3D parentWorldPos;
                                if (pair.Value.Parent != null)
                                    parentWorldPos = pair.Value.Parent.BaseTransform * (new Vector3D(0));
                                else
                                    parentWorldPos = new Vector3D(0);
                                Vector3D offset = worldPos - parentWorldPos;
                                offsetList.Add(offset);
                            }
                            int k = 0;
                            foreach (var pair in FOut[i].JointTable)
                            {
                                pair.Value.BaseTransform = VMath.Translate(offsetList[k]);
                                k++;
                            }
                        }
                    }
                }
            }
            else
            {
                FOut.Stream.IsChanged = false;
            }
        }
    }
}
