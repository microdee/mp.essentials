using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D9;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.SkeletonV2
{
    [PluginInfo(
         Name = "MixPose",
         Category = "Skeleton",
         Version = "V2",
         Author = "microdee"
     )]
    public class V2SkeletonMixPose : ConfigurableDynamicPinNode<int>, IPluginEvaluate
    {
        [Config("Pose Count", DefaultValue = 2)] public IDiffSpread<int> FPoseCount;
        [Input("Animation Only", Order = 10000, DefaultBoolean = true)]
        public ISpread<bool> FAnimOnly;
        [Output("Skeleton")] public ISpread<ISkeleton> FOut;

        [Import] public IIOFactory FIOFactory;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FPoseCount;
            Pins = new PinDictionary(FIOFactory);
        }

        protected override bool IsConfigDefault()
        {
            return FPoseCount[0] == 0;
        }

        protected override void OnConfigPinChanged()
        {
            var currposecount = Pins.InputPins.Count / 2;
            if (currposecount < FPoseCount[0])
            {
                for (int i = currposecount; i < FPoseCount[0]; i++)
                {
                    Pins.AddInput(typeof(ISkeleton), new InputAttribute($"Pose {i}")
                    {
                        Order = i * 2
                    }, i);
                    Pins.AddInput(typeof(double), new InputAttribute($"Amount {i}")
                    {
                        Order = i * 2 + 1,
                        DefaultValue = 1
                    }, i);
                }
            }
            if (currposecount > FPoseCount[0])
            {
                for (int i = FPoseCount[0]; i < currposecount; i++)
                {
                    Pins.RemoveInput($"Amount {i}");
                    Pins.RemoveInput($"Pose {i}");
                }
            }
        }

        protected PinDictionary Pins;

        public void Evaluate(int SpreadMax)
        {
            if (Pins.InputPins.Count == 0 || Pins.InputSpreadMin == 0)
            {
                FOut.SliceCount = 0;
                return;
            }
            if (Pins.InputPins.Values.Any(pin => pin.Spread[0] == null)) return;
            if (Pins.InputChanged)
            {
                FOut.Stream.IsChanged = true;
                var currposecount = Pins.InputPins.Count / 2;
                for (int i = 0; i < FOut.SliceCount; i++)
                {
                    if (FOut[i] == null) FOut[i] = new Skeleton();
                    else ((ISkeleton)Pins.InputPins["Pose 0"].Spread[i]).CopyData(FOut[i]);
                    var outSkeleton = FOut[i];
                    var outAmount = (double)Pins.InputPins["Amount 0"].Spread[i];
                    var outAmDiv = 1;
                    for (int j = 1; j < currposecount; j++)
                    {
                        var currSkeleton = (ISkeleton)Pins.InputPins[$"Pose {j}"].Spread[i];
                        var currAmount = (double)Pins.InputPins[$"Amount {j}"].Spread[i];
                        foreach (var joint in outSkeleton.JointTable.Keys)
                        {
                            if(!currSkeleton.JointTable.ContainsKey(joint)) continue;

                            Matrix4x4 result;
                            if (!FAnimOnly[0])
                            {
                                var currBase = currSkeleton.JointTable[joint].BaseTransform;
                                var outBase = outSkeleton.JointTable[joint].BaseTransform;
                                Matrix4x4Utils.Blend(outBase, currBase, outAmount / outAmDiv, currAmount, out result);
                                outSkeleton.JointTable[joint].BaseTransform = result;
                            }

                            var currAnim = currSkeleton.JointTable[joint].AnimationTransform;
                            var outAnim = outSkeleton.JointTable[joint].AnimationTransform;
                            Matrix4x4Utils.Blend(outAnim, currAnim, outAmount / outAmDiv, currAmount, out result);
                            outSkeleton.JointTable[joint].AnimationTransform = result;
                        }
                        outAmount += currAmount;
                        outAmDiv++;
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
