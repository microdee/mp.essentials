using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction;
using md.stdl.Interaction.Notui;
using md.stdl.Mathematics;
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
    public class ContextNode : IPluginEvaluate
    {
        [Import] public IPluginHost2 PluginHost;
        [Import] public IHDEHost Host;

        [Input("Element Prototypes")]
        public Pin<IGuiElement> FElements;
        [Input("Single Source of Elements")]
        public ISpread<bool> FSingleSource;
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
        public IDiffSpread<VMatrix> FViewTr;
        [Input("Projection")]
        public IDiffSpread<VMatrix> FProjTr;
        [Input("Aspect Ratio")]
        public IDiffSpread<VMatrix> FAspTr;

        [Output("Context")]
        public ISpread<NotuiContext> FContext;
        [Output("Hierarchical Elements")]
        public ISpread<IGuiElement> FElementsOut;
        [Output("Flat Elements")]
        public ISpread<IGuiElement> FFlatElements;
        
        [Output("Touches")]
        public ISpread<TouchContainer> FTouches;

        public NotuiContext Context = new NotuiContext();
        protected Dictionary<Guid, IGuiElement> ElementInstances = new Dictionary<Guid, IGuiElement>();

        private double _prevFrameTime = 0;

        public bool IsTouchDefault()
        {
            bool def = FTouchCoords.SliceCount == 1 && FTouchId.SliceCount == 1 && FTouchForce.SliceCount == 1;
            def = def && FTouchCoords[0].Length < 0.00001;
            def = def && FTouchId[0] == 0;
            def = def && FTouchForce[0] < 0.00001;
            return def;
        }

        public void InstantiateChildren(IGuiElement elementinstance, IGuiElement prototype)
        {
            elementinstance.Id = prototype.Id;
            if (!(prototype.Value.Auxiliary is ContextInstanceGetter contextgetter))
            {
                contextgetter = new ContextInstanceGetter();
            }
            if(!contextgetter.Instances.ContainsKey(Context))
                contextgetter.Instances.Add(Context, elementinstance);
            prototype.Value.Auxiliary = contextgetter;
            for (int i = 0; i < prototype.Children.Count; i++)
            {
                InstantiateChildren(elementinstance.Children[i], prototype.Children[i]);
            }
        }

        public void Evaluate(int SpreadMax)
        {
            var dt = Host.FrameTime - _prevFrameTime;
            if (_prevFrameTime <= 0.00001) dt = 0;

            Context.View = FViewTr[0].AsSystemMatrix4X4();
            Context.Projection = FProjTr[0].AsSystemMatrix4X4();
            Context.AspectRatio = FAspTr[0].AsSystemMatrix4X4();

            if (FElements.IsConnected && FElements.IsChanged)
            {
                foreach (var element in FElements)
                {
                    if (!ElementInstances.ContainsKey(element.Id))
                    {
                        var elementinstance = element.Copy();
                        InstantiateChildren(elementinstance, element);
                        ElementInstances.Add(element.Id, elementinstance);
                    }
                    else
                    {
                        element.UpdateTo(ElementInstances[element.Id]);
                    }
                }

                var removables = (from elementid in ElementInstances.Keys
                    where FElements.All(el => el.Id != elementid)
                    select elementid).ToArray();
                
                foreach (var elid in removables)
                {
                    ElementInstances.Remove(elid);
                }

                Context.AddOrUpdateElements(FSingleSource[0], ElementInstances.Values.ToArray());
            }

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

            FFlatElements.AssignFrom(Context.FlatElementList);
            FElementsOut.AssignFrom(Context.Elements);
            FTouches.AssignFrom(Context.Touches.Values);

            _prevFrameTime = Host.FrameTime;
        }
    }
}
