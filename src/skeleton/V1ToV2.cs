using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Nodes;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.SkeletonInterfaces;

namespace VVVV.Nodes.SkeletonV2
{
    [PluginInfo(
        Name = "V1ToV2",
        Category = "Skeleton",
        Author = "microdee"
    )]
    public class SkeletonV1ToV2Node : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        public IPluginHost FHost;
        protected INodeIn V1SkeletonIn;

        [Input("Copy", Order = 2)]
        public IDiffSpread<bool> FCopy;

        [Output("Skeleton")]
        public ISpread<ISkeleton> FOut;

        public void OnImportsSatisfied()
        {
            var guids = new[] { SkeletonNodeIO.GUID };
            FHost.CreateNodeInput("V1 Skeleton", TSliceMode.Single, TPinVisibility.True, out V1SkeletonIn);
            V1SkeletonIn.SetSubType(guids, "Skeleton");
        }

        public void Evaluate(int SpreadMax)
        {
            if (V1SkeletonIn.SliceCount == 0)
            {
                FOut.SliceCount = 0;
                return;
            }
            if (V1SkeletonIn.PinIsChanged || FCopy.IsChanged)
            {
                if (!V1SkeletonIn.IsConnected)
                {
                    FOut.Stream.IsChanged = false;
                    return;
                }
                object usinterface;
                V1SkeletonIn.GetUpstreamInterface(out usinterface);
                var skeletonin = (ISkeleton) usinterface;
                if (FCopy[0]) FOut[0] = skeletonin.DeepCopy();
                else FOut[0] = skeletonin;
            }
            else
            {
                FOut.Stream.IsChanged = false;
            }
        }
    }
    [PluginInfo(
        Name = "V2ToV1",
        Category = "Skeleton",
        Author = "microdee"
    )]
    public class SkeletonV2ToV1Node : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        public IPluginHost FHost;
        protected INodeOut V1SkeletonOut;

        [Input("Skeleton")]
        public Pin<ISkeleton> FInput;

        [Input("Copy", Order = 2)]
        public IDiffSpread<bool> FCopy;


        public void OnImportsSatisfied()
        {
            var guids = new[] { SkeletonNodeIO.GUID };
            FHost.CreateNodeOutput("V1 Skeleton", TSliceMode.Single, TPinVisibility.True, out V1SkeletonOut);
            V1SkeletonOut.SetSubType(guids, "Skeleton");
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.SliceCount == 0)
            {
                V1SkeletonOut.SliceCount = 0;
                return;
            }
            if (!FInput.IsChanged && !FCopy.IsChanged) return;
            if (!FInput.IsConnected) return;
            if (FCopy[0]) V1SkeletonOut.SetInterface(FInput[0].DeepCopy());
            else V1SkeletonOut.SetInterface(FInput[0]);
            V1SkeletonOut.MarkPinAsChanged();
        }
    }
}
