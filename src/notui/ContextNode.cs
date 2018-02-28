using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
using VMatrix = VVVV.Utils.VMath.Matrix4x4;
using SMatrix = System.Numerics.Matrix4x4;


namespace mp.essentials.notui
{
    [PluginInfo(
        Name = "Context",
        Category = "Notui",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class ContextNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IPluginFeedbackLoop
    {
        [Import] public IPluginHost2 PluginHost;
        [Import] public IHDEHost Host;

        [Input("Element Prototypes")]
        public Pin<ElementPrototype> FElements;
        //[Input("Single Source of Elements")]
        //public ISpread<bool> FSingleSource;
        [Input("Touch Coordinates")]
        public IDiffSpread<Vector2D> FTouchCoords;
        [Input("Touch ID")]
        public IDiffSpread<int> FTouchId;
        [Input("Touch Force")]
        public IDiffSpread<float> FTouchForce;
        [Input("Consider Touch New Before")]
        public ISpread<int> FConsiderNew;
        [Input("Consider Touch Released After")]
        public ISpread<int> FConsiderReleased;

        [Input("View")]
        public Pin<VMatrix> FViewTr;
        [Input("Projection")]
        public Pin<VMatrix> FProjTr;
        [Input("Aspect Ratio")]
        public Pin<VMatrix> FAspTr;

        [Output("Context")]
        public ISpread<NotuiContext> FContext;
        [Output("Hierarchical Elements")]
        public ISpread<NotuiElement> FElementsOut;
        [Output("Flat Elements")]
        public ISpread<NotuiElement> FFlatElements;
        
        [Output("Touches")]
        public ISpread<TouchContainer> FTouches;

        public NotuiContext Context = new NotuiContext();

        private double _prevFrameTime = 0;

        public bool IsTouchDefault()
        {
            bool def = FTouchCoords.SliceCount == 1 && FTouchId.SliceCount == 1 && FTouchForce.SliceCount == 1;
            def = def && FTouchCoords[0].Length < 0.00001;
            def = def && FTouchId[0] == 0;
            def = def && FTouchForce[0] < 0.00001;
            return def;
        }

        private bool _onConnectedFrame;

        public void OnImportsSatisfied()
        {
            FElements.Connected += (sender, args) => _onConnectedFrame = true;
            FElements.Disconnected += (sender, args) =>
            {
                Context.AddOrUpdateElements(true); // this is basically asking all elements to request their deletion
            };
        }

        public void Evaluate(int SpreadMax)
        {
            var dt = Host.FrameTime - _prevFrameTime;
            if (_prevFrameTime <= 0.00001) dt = 0;
            if(FViewTr.IsConnected && FViewTr.SliceCount > 0)
                Context.View = FViewTr[0].AsSystemMatrix4X4();
            if (FProjTr.IsConnected && FProjTr.SliceCount > 0)
                Context.Projection = FProjTr[0].AsSystemMatrix4X4();
            if (FAspTr.IsConnected && FAspTr.SliceCount > 0)
                Context.AspectRatio = FAspTr[0].AsSystemMatrix4X4();
            if(FElements.IsChanged)
                Context.AddOrUpdateElements(true, FElements.ToArray());

            var touchcount = Math.Min(FTouchId.SliceCount, FTouchCoords.SliceCount);
            var touchlist = new List<TouchPrototype>();
            if (!IsTouchDefault())
            {
                for (int i = 0; i < touchcount; i++)
                {
                    touchlist.Add(new TouchPrototype
                    {
                        Point = FTouchCoords[i].AsSystemVector(),
                        Id = FTouchId[i],
                        Force = FTouchForce[i]
                    });
                }
            }
            Context.Mainloop(touchlist, (float)dt);

            FContext[0] = Context;
            FFlatElements.AssignFrom(Context.FlatElements);
            FElementsOut.AssignFrom(Context.RootElements.Values);
            FTouches.AssignFrom(Context.Touches.Values);

            _prevFrameTime = Host.FrameTime;
        }

        public bool OutputRequiresInputEvaluation(IPluginIO inputPin, IPluginIO outputPin)
        {
            return false;
        }
    }

    [PluginInfo(
        Name = "Context",
        Category = "Notui",
        Version = "Split",
        Author = "microdee"
    )]
    public class ContextSplitNode : ObjectSplitNode<NotuiContext>
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
}
