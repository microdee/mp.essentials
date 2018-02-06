using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction;
using md.stdl.Interaction.Notui;
using md.stdl.Mathematics;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using ISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;

namespace mp.essentials.notui
{
    [PluginInfo(
        Name = "AttachedValues",
        Category = "Notui",
        Version = "Split",
        Author = "microdee"
    )]
    public class AttachedValuesSplitNodes : ObjectSplitNode<AttachedValues> { }

    [PluginInfo(
        Name = "Info",
        Category = "Notui.Element",
        Version = "Split",
        Author = "microdee"
    )]
    public class GuiElementInfoNode : IPluginEvaluate
    {
        [Input("Element")] public Pin<IGuiElement> FElement;

        [Output("Element")] public ISpread<IGuiElement> FElementOut;
        [Output("Name Out")] public ISpread<string> FNameOut;
        [Output("ID")] public ISpread<string> FId;
        [Output("Hit")] public ISpread<bool> FHit;
        [Output("Touched")] public ISpread<bool> FTouched;
        [Output("Active Out")] public ISpread<bool> FActiveOut;
        [Output("Transparent Out")] public ISpread<bool> FTransparentOut;
        [Output("Depth")] public ISpread<float> FDepth;
        [Output("Fade Out Duration")] public ISpread<float> FFadeOutDur;
        [Output("Fade In Duration")] public ISpread<float> FFadeInDur;
        [Output("Fade Progress")] public ISpread<float> FElementFade;
        [Output("Age")] public ISpread<double> FAge;
        [Output("Dethklok")] public ISpread<double> FDethklok;
        [Output("Attached Values")] public ISpread<AttachedValues> FAttachedVals;

        [Output("Interacting Touches")] public ISpread<ISpread<TouchContainer>> FTouches;
        [Output("Are Interacting Touches Hitting")] public ISpread<ISpread<bool>> FTouchesHitting;
        [Output("Hitting Touches")] public ISpread<ISpread<TouchContainer>> FHittingTouches;
        [Output("Hitting Intersections")] public ISpread<ISpread<IntersectionPoint>> FHittingIntersections;
        [Output("Children Out")] public ISpread<ISpread<IGuiElement>> FChildrenOut;
        [Output("Behaviors Out")] public ISpread<ISpread<IInteractionBehavior>> FBehavsOut;
        [Output("Parent")] public ISpread<ISpread<IGuiElement>> FParent;
        [Output("Context")] public ISpread<ISpread<NotuiContext>> FContext;
        
        [Output("Interaction Transformation Out")] public ISpread<Matrix4x4> FInterTrOut;
        [Output("Display Transformation Out")] public ISpread<Matrix4x4> FDisplayTrOut;
        
        protected void AssignElementOutputs(IGuiElement element, int i)
        {
            FElementOut[i] = element;
            FNameOut[i] = element.Name;
            FId[i] = element.Id.ToString();
            FHit[i] = element.Hit;
            FTouched[i] = element.Touched;
            FActiveOut[i] = element.Active;
            FTransparentOut[i] = element.Transparent;
            FDepth[i] = element.Depth;
            FFadeOutDur[i] = element.FadeOutTime;
            FFadeInDur[i] = element.FadeInTime;
            FElementFade[i] = element.ElementFade;
            FAge[i] = element.Age.Elapsed.TotalSeconds;
            FDethklok[i] = element.Dethklok.Elapsed.TotalSeconds;
            FAttachedVals[i] = element.Value;

            FTouches[i].AssignFrom(element.Touching.Keys);
            FTouchesHitting[i].AssignFrom(element.Touching.Values.Select(t => t != null));
            FHittingTouches[i].AssignFrom(element.Hitting.Keys);
            FHittingIntersections[i].AssignFrom(element.Hitting.Values);
            FChildrenOut[i].AssignFrom(element.Children);
            FBehavsOut[i].AssignFrom(element.Behaviors);

            if (element.Parent == null)
                FParent[i].SliceCount = 0;
            else
            {
                FParent[i].SliceCount = 1;
                FParent[i][0] = element.Parent;
            }

            if (element.Context == null)
                FContext[i].SliceCount = 0;
            else
            {
                FContext[i].SliceCount = 1;
                FContext[i][0] = element.Context;
            }

            FInterTrOut[i] = element.InteractionMatrix.AsVMatrix4X4();
            FDisplayTrOut[i] = element.DisplayMatrix.AsVMatrix4X4();
        }

        protected int _prevSliceCount;

        public void Evaluate(int SpreadMax)
        {
            if (FElement.IsConnected)
            {
                if(_prevSliceCount != FElement.SliceCount)
                    this.SetSliceCountForAllOutput(FElement.SliceCount);
                _prevSliceCount = FElement.SliceCount;

                for (int i = 0; i < FElement.SliceCount; i++)
                {
                    AssignElementOutputs(FElement[i], i);
                }
            }
            else
            {
                this.SetSliceCountForAllOutput(0);
            }
        }
    }
}
