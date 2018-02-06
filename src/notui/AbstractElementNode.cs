using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;
using md.stdl.Mathematics;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VMath;
using VMatrix = VVVV.Utils.VMath.Matrix4x4;
using SMatrix = System.Numerics.Matrix4x4;

namespace mp.essentials.notui
{
    public class ContextInstanceGetter : ICloneable
    {
        public Dictionary<NotuiContext, IGuiElement> Instances = new Dictionary<NotuiContext, IGuiElement>();
        public object Clone()
        {
            return new ContextInstanceGetter
            {
                Instances = Instances
            };
        }
    }

    public abstract class AbstractElementNode<TElement> : IPluginEvaluate where TElement : IGuiElement, new()
    {
        [Import] public IPluginHost2 PluginHost;
        [Import] public IHDEHost Host;
        
        [Input("Children")]
        public Pin<ISpread<IGuiElement>> FChildren;
        [Input("Name", DefaultString = "callmenames")]
        public IDiffSpread<string> FName;
        [Input("Fade In Time")]
        public IDiffSpread<float> FFadeIn;
        [Input("Fade Out Time")]
        public IDiffSpread<float> FFadeOut;
        [Input("Behaviors")]
        public Pin<ISpread<IInteractionBehavior>> FBehaviors;
        [Input("Transparent")]
        public IDiffSpread<bool> FTransparent;
        [Input("Active", DefaultBoolean = true)]
        public IDiffSpread<bool> FActive;

        [Input("Display Transform")]
        public IDiffSpread<VMatrix> FDispTr;
        [Input("Interaction Transform")]
        public IDiffSpread<VMatrix> FInterTr;
        [Input("Separate Interaction from Display")]
        public IDiffSpread<bool> FSeparateInter;

        [Output("Element Prototype")]
        public ISpread<TElement> FElementProt;

        [Output("Element Context")]
        public ISpread<ISpread<NotuiContext>> FElementContext;
        [Output("Element Instances")]
        public ISpread<ISpread<IGuiElement>> FElementInst;

        protected virtual void FillElementAuxData(TElement el, int i) { }

        protected TElement FillElement(TElement el, int i)
        {
            FDispTr[i].Decompose(out var scale, out Vector4D rotation, out var pos);
            el.Name = FName[i];
            el.FadeInTime = FFadeIn[i];
            el.FadeOutTime = FFadeOut[i];
            el.Active = FActive[i];
            el.Transparent = FTransparent[i];
            if (FBehaviors.IsConnected)
            {
                el.Behaviors = FBehaviors[i].Select(b =>
                {
                    var bc = b.Copy();
                    bc.AttachedElement = el;
                    return bc;
                }).ToList();
            }
            el.DisplayTransformation.Position = pos.AsSystemVector();
            el.DisplayTransformation.Rotation = rotation.AsSystemQuaternion();
            el.DisplayTransformation.Scale = scale.AsSystemVector();
            if (FSeparateInter[i])
            {
                FInterTr[i].Decompose(out var iscale, out Vector4D irotation, out var ipos);
                el.InteractionTransformation.Position = ipos.AsSystemVector();
                el.InteractionTransformation.Rotation = irotation.AsSystemQuaternion();
                el.InteractionTransformation.Scale = iscale.AsSystemVector();
            }
            else
            {
                el.DisplayTransformation.CopyTo(el.InteractionTransformation);
            }
            if(FChildren.IsConnected) el.AddOrUpdateChildren(true, FChildren[i].ToArray());
            FillElementAuxData(el, i);

            return el;
        }

        private bool init = true;

        public void Evaluate(int SpreadMax)
        {
            bool changed = FChildren.IsChanged || FName.IsChanged || FFadeIn.IsChanged || FFadeOut.IsChanged ||
                           FBehaviors.IsChanged || FDispTr.IsChanged || FInterTr.IsChanged || FSeparateInter.IsChanged ||
                           FTransparent.IsChanged || FActive.IsChanged;

            int sprmax = SpreadUtils.SpreadMax(FChildren, FName, FBehaviors, FDispTr, FInterTr);

            FElementProt.Stream.IsChanged = false;

            if (changed || init)
            {
                if (init) FElementProt.SliceCount = 0;
                if (!init)
                {
                    for (int i = 0; i < FElementProt.SliceCount; i++)
                    {
                        FillElement(FElementProt[i], i);
                    }
                }
                FElementProt.ResizeAndDismiss(sprmax, i => FillElement(new TElement(), i));
                FElementProt.Stream.IsChanged = true;
                init = false;
            }

            FElementInst.SliceCount = FElementContext.SliceCount = sprmax;
            for (int i = 0; i < sprmax; i++)
            {
                if (FElementProt[i].Value.Auxiliary is ContextInstanceGetter contextgetter)
                {
                    FElementContext[i].SliceCount = FElementInst[i].SliceCount = contextgetter.Instances.Count;
                    int ii = 0;
                    foreach (var contextElementPair in contextgetter.Instances)
                    {
                        FElementInst[i][ii] = contextElementPair.Value;
                        FElementContext[i][ii] = contextElementPair.Key;
                        ii++;
                    }
                }
                else
                {
                    FElementContext[i].SliceCount = FElementInst[i].SliceCount = 0;
                }
            }
        }
    }
}
