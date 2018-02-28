using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using md.stdl.Interaction;
using md.stdl.Interaction.Notui;
using md.stdl.Mathematics;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Reflection;
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
        Name = "ElementTransformation",
        Category = "Notui",
        Version = "Split",
        Author = "microdee"
    )]
    public class ElementTransformationSplitNodes : ObjectSplitNode<ElementTransformation>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            return MiscExtensions.MapSystemNumericsTypeToVVVV(original);
        }
        public override object TransformOutput(object obj, MemberInfo member, int i)
        {
            return MiscExtensions.MapSystemNumericsValueToVVVV(obj);
        }
    }

    [PluginInfo(
        Name = "IntersectionPoint",
        Category = "Notui",
        Version = "Split",
        Author = "microdee"
    )]
    public class IntersectionPointSplitNodes : ObjectSplitNode<IntersectionPoint>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            return MiscExtensions.MapSystemNumericsTypeToVVVV(original);
        }
        public override object TransformOutput(object obj, MemberInfo member, int i)
        {
            return MiscExtensions.MapSystemNumericsValueToVVVV(obj);
        }
    }

    [PluginInfo(
        Name = "PrototypeInfo",
        Category = "Notui.ElementPrototype",
        Version = "Split",
        Author = "microdee"
    )]
    public class PrototypeInfoSplitNodes : ObjectSplitNode<ElementPrototype>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            if (original.Is(typeof(Stopwatch)))
            {
                return typeof(double);
            }
            return MiscExtensions.MapSystemNumericsTypeToVVVV(original);
        }
        public override object TransformOutput(object obj, MemberInfo member, int i)
        {
            if (obj is Stopwatch s)
            {
                return s.Elapsed.TotalSeconds;
            }
            return MiscExtensions.MapSystemNumericsValueToVVVV(obj);
        }
    }

    [PluginInfo(
        Name = "InstanceInfo",
        Category = "Notui.Element",
        Version = "Split",
        Author = "microdee"
    )]
    public class GuiElementInfoNode : IPluginEvaluate
    {
        [Input("Element")] public Pin<NotuiElement> FElement;

        [Output("Element")] public ISpread<NotuiElement> FElementOut;
        [Output("Type")] public ISpread<string> FType;
        [Output("Name Out")] public ISpread<string> FNameOut;
        [Output("ID")] public ISpread<string> FId;
        [Output("Hit")] public ISpread<bool> FHit;
        [Output("Touched")] public ISpread<bool> FTouched;
        [Output("Active Out")] public ISpread<bool> FActiveOut;
        [Output("Transparent Out")] public ISpread<bool> FTransparentOut;
        [Output("Fade Out Duration")] public ISpread<float> FFadeOutDur;
        [Output("Fade In Duration")] public ISpread<float> FFadeInDur;
        [Output("Fade Progress")] public ISpread<float> FElementFade;
        [Output("Age")] public ISpread<double> FAge;
        [Output("Dethklok")] public ISpread<double> FDethklok;
        [Output("Dying")] public ISpread<bool> FDying;
        [Output("Attached Values")] public ISpread<AttachedValues> FAttachedVals;

        [Output("Interacting Touches")] public ISpread<ISpread<TouchContainer>> FTouches;
        [Output("Are Interacting Touches Hitting")] public ISpread<ISpread<bool>> FTouchesHitting;
        [Output("Touching Intersections")] public ISpread<ISpread<IntersectionPoint>> FTouchingIntersections;
        [Output("Hitting Touches")] public ISpread<ISpread<TouchContainer>> FHittingTouches;
        [Output("Hitting Intersections")] public ISpread<ISpread<IntersectionPoint>> FHittingIntersections;
        [Output("Children Out")] public ISpread<ISpread<NotuiElement>> FChildrenOut;
        [Output("Behaviors Out")] public ISpread<ISpread<InteractionBehavior>> FBehavsOut;
        [Output("Parent")] public ISpread<ISpread<NotuiElement>> FParent;
        [Output("Context")] public ISpread<ISpread<NotuiContext>> FContext;
        
        [Output("Interaction Transformation Out")] public ISpread<Matrix4x4> FInterTrOut;
        [Output("Display Transformation Out")] public ISpread<Matrix4x4> FDisplayTrOut;
        
        protected void AssignElementOutputs(NotuiElement element, int i)
        {
            FElementOut[i] = element;
            FNameOut[i] = element.Name;
            FType[i] = element.GetType().GetCSharpName();
            FId[i] = element.Id;
            FHit[i] = element.Hit;
            FTouched[i] = element.Touched;
            FActiveOut[i] = element.Active;
            FTransparentOut[i] = element.Transparent;
            FFadeOutDur[i] = element.FadeOutTime;
            FFadeInDur[i] = element.FadeInTime;
            FElementFade[i] = element.ElementFade;
            FAge[i] = element.Age.Elapsed.TotalSeconds;
            FDying[i] = element.Dying;
            FDethklok[i] = element.Dethklok.Elapsed.TotalSeconds;
            FAttachedVals[i] = element.Value;

            FTouches[i].AssignFrom(element.Touching.Keys);
            FTouchesHitting[i].AssignFrom(element.Touching.Values.Select(t => t != null));
            FTouchingIntersections[i].AssignFrom(element.Touching.Values.Where(t => t != null));
            FHittingTouches[i].AssignFrom(element.Hitting.Keys);
            FHittingIntersections[i].AssignFrom(element.Hitting.Values);
            FChildrenOut[i].AssignFrom(element.Children.Values);
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
            if (FElement.IsConnected && FElement.SliceCount > 0 && FElement[0] != null)
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
