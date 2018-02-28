using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.notui
{
    [PluginInfo(
        Name = "Events",
        Category = "Notui.Element",
        Author = "microdee"
    )]
    public class GuiElementEventsSplitNodePart : IPluginEvaluate, IPluginAwareOfEvaluation, IPartImportsSatisfiedNotification
    {
        [Import] public IPluginHost2 PluginHost;
        [Import] public IHDEHost Host;

        [Input("Element")] public Pin<NotuiElement> FElement;
        private readonly Spread<NotuiElement> PrevElements = new Spread<NotuiElement>();

        [Output("On Interaction Begin", IsBang = true)] public ISpread<bool> FOnInteractionBegin;
        [Output("On Interaction End", IsBang = true)] public ISpread<bool> FOnInteractionEnd;
        [Output("On Touch Begin", IsBang = true)] public ISpread<bool> FOnTouchBegin;
        [Output("On Touch End", IsBang = true)] public ISpread<bool> FOnTouchEnd;
        [Output("On Hit Begin", IsBang = true)] public ISpread<bool> FOnHitBegin;
        [Output("On Hit End", IsBang = true)] public ISpread<bool> FOnHitEnd;
        [Output("Interacting")] public ISpread<bool> FOnInteracting;
        [Output("On Children Added", IsBang = true)] public ISpread<bool> FOnChildrenUpdated;
        [Output("On Deletion Started", IsBang = true)] public ISpread<bool> FOnDeletionStarted;
        [Output("Deleting", IsBang = true)] public ISpread<bool> FOnDeleting;
        [Output("Faded In", IsBang = true)] public ISpread<bool> FOnFadedIn;

        private Dictionary<ISpread<bool>, Spread<bool>> Outputs;
        private string NodePath = "";

        private void HandledEvent(object sender, ISpread<bool> spread)
        {
            var element = (NotuiElement)sender;
            if (element.EnvironmentObject is VEnvironmentData venvdat)
            {
                if(!venvdat.NodeSpecific.ContainsKey(NodePath)) return;
                if(!(venvdat.NodeSpecific[NodePath] is int)) return;

                var i = (int)venvdat.NodeSpecific[NodePath];
                spread[i] = true;
                Outputs[spread][i] = true;
            }
        }

        public void OnInteractionBegin(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnInteractionBegin);
        public void OnInteractionEnd(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnInteractionEnd);
        public void OnTouchBegin(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnTouchBegin);
        public void OnTouchEnd(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnTouchEnd);
        public void OnHitBegin(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnHitBegin);
        public void OnHitEnd(object sender, TouchInteractionEventArgs args) => HandledEvent(sender, FOnHitEnd);
        public void OnInteracting(object sender, EventArgs args) => HandledEvent(sender, FOnInteracting);
        public void OnChildrenUpdated(object sender, ChildrenUpdatedEventArgs args) => HandledEvent(sender, FOnChildrenUpdated);
        public void OnDeletionStarted(object sender, EventArgs args) => HandledEvent(sender, FOnDeletionStarted);
        public void OnDeleting(object sender, EventArgs args) => HandledEvent(sender, FOnDeleting);
        public void OnFadedIn(object sender, EventArgs args) => HandledEvent(sender, FOnFadedIn);

        private void Subscribe(NotuiElement element, int i)
        {
            element.AttachSliceId(NodePath, i);

            element.OnInteractionBegin += OnInteractionBegin;
            element.OnInteractionEnd += OnInteractionEnd;
            element.OnTouchBegin += OnTouchBegin;
            element.OnTouchEnd += OnTouchEnd;
            element.OnHitBegin += OnHitBegin;
            element.OnHitEnd += OnHitEnd;
            element.OnInteracting += OnInteracting;
            element.OnChildrenUpdated += OnChildrenUpdated;
            element.OnDeletionStarted += OnDeletionStarted;
            element.OnDeleting += OnDeleting;
            element.OnFadedIn += OnFadedIn;
        }
        private void UnSubscribe(NotuiElement element, int i)
        {
            element.AttachSliceId(NodePath, i);

            element.OnInteractionBegin -= OnInteractionBegin;
            element.OnInteractionEnd -= OnInteractionEnd;
            element.OnTouchBegin -= OnTouchBegin;
            element.OnTouchEnd -= OnTouchEnd;
            element.OnHitBegin -= OnHitBegin;
            element.OnHitEnd -= OnHitEnd;
            element.OnInteracting -= OnInteracting;
            element.OnChildrenUpdated -= OnChildrenUpdated;
            element.OnDeletionStarted -= OnDeletionStarted;
            element.OnDeleting -= OnDeleting;
            element.OnFadedIn -= OnFadedIn;
        }

        public void OnImportsSatisfied()
        {
            PluginHost.GetNodePath(false, out NodePath);
            Outputs = new Dictionary<ISpread<bool>, Spread<bool>>
            {
                {FOnInteractionBegin, new Spread<bool>()},
                {FOnInteractionEnd, new Spread<bool>()},
                {FOnTouchBegin, new Spread<bool>()},
                {FOnTouchEnd, new Spread<bool>()},
                {FOnHitBegin, new Spread<bool>()},
                {FOnHitEnd, new Spread<bool>()},
                {FOnInteracting, new Spread<bool>()},
                {FOnChildrenUpdated, new Spread<bool>()},
                {FOnDeletionStarted, new Spread<bool>()},
                {FOnDeleting, new Spread<bool>()},
                {FOnFadedIn, new Spread<bool>()}
            };
        }

        public void Evaluate(int SpreadMax)
        {
            if (FElement.IsConnected)
            {
                if (FElement.SliceCount != PrevElements.SliceCount)
                {
                    foreach (var kvp in Outputs)
                    {
                        kvp.Key.SliceCount = kvp.Value.SliceCount = FElement.SliceCount;
                    }
                    PrevElements.SliceCount = FElement.SliceCount;
                }

                for (int i = 0; i < FElement.SliceCount; i++)
                {
                    if (PrevElements[i] == null)
                    {
                        Subscribe(FElement[i], i);
                    }
                    else if (PrevElements[i].Id != FElement[i].Id)
                    {
                        UnSubscribe(PrevElements[i], i);
                        Subscribe(FElement[i], i);
                    }

                    PrevElements[i] = FElement[i];

                    foreach (var kvp in Outputs)
                    {
                        if (kvp.Value[i] ^ kvp.Key[i])
                        {
                            kvp.Value[i] = kvp.Key[i] = false;
                        }
                        if (kvp.Value[i])
                            kvp.Value[i] = false;
                    }
                }
            }
            else
            {
                foreach (var kvp in Outputs)
                {
                    kvp.Key.SliceCount = kvp.Value.SliceCount = 0;
                }
            }
        }

        public void TurnOn()
        {
            int i = 0;
            foreach (var element in FElement)
            {
                Subscribe(element, i);
                i++;
            }
        }

        public void TurnOff()
        {
            int i = 0;
            foreach (var element in FElement)
            {
                UnSubscribe(element, i);
                i++;
            }
        }
    }
}
