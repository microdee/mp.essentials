using System;
using System.Collections.Generic;
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
    public class GuiElementEventsSplitNodePart : IPluginEvaluate, IPluginAwareOfEvaluation
    {
        [Input("Element")] public Pin<IGuiElement> FElement;
        private readonly Spread<IGuiElement> PrevElements = new Spread<IGuiElement>();

        [Output("On Interaction Begin", IsBang = true)] public ISpread<bool> FOnInteractionBegin;
        [Output("On Interaction End", IsBang = true)] public ISpread<bool> FOnInteractionEnd;
        [Output("On Touch Begin", IsBang = true)] public ISpread<bool> FOnTouchBegin;
        [Output("On Touch End", IsBang = true)] public ISpread<bool> FOnTouchEnd;
        [Output("On Hit Begin", IsBang = true)] public ISpread<bool> FOnHitBegin;
        [Output("On Hit End", IsBang = true)] public ISpread<bool> FOnHitEnd;
        [Output("Interacting")] public ISpread<bool> FOnInteracting;
        [Output("On Children Added", IsBang = true)] public ISpread<bool> FOnChildrenAdded;
        [Output("On Deletion Started", IsBang = true)] public ISpread<bool> FOnDeletionStarted;
        [Output("Deleting", IsBang = true)] public ISpread<bool> FOnDeleting;
        [Output("Faded In", IsBang = true)] public ISpread<bool> FOnFadedIn;

        private HashSet<ISpread> Outputs;

        public void OnInteractionBegin(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnInteractionBegin[i] = true;
            FOnInteractionBegin.Stream.IsChanged = true;
        }

        public void OnInteractionEnd(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnInteractionEnd[i] = true;
            FOnInteractionEnd.Stream.IsChanged = true;
        }

        public void OnTouchBegin(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnTouchBegin[i] = true;
            FOnTouchBegin.Stream.IsChanged = true;
        }

        public void OnTouchEnd(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnTouchEnd[i] = true;
            FOnTouchEnd.Stream.IsChanged = true;
        }

        public void OnHitBegin(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnHitBegin[i] = true;
            FOnHitBegin.Stream.IsChanged = true;
        }

        public void OnHitEnd(object sender, TouchInteractionEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnHitEnd[i] = true;
            FOnHitEnd.Stream.IsChanged = true;
        }

        public void OnInteracting(object sender, EventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnInteracting[i] = true;
            FOnInteracting.Stream.IsChanged = true;
        }

        public void OnChildrenAdded(object sender, ChildrenAddedEventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnChildrenAdded[i] = true;
            FOnChildrenAdded.Stream.IsChanged = true;
        }

        public void OnDeletionStarted(object sender, EventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnDeletionStarted[i] = true;
            FOnDeletionStarted.Stream.IsChanged = true;
        }

        public void OnDeleting(object sender, EventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnDeleting[i] = true;
            FOnDeleting.Stream.IsChanged = true;
        }

        public void OnFadedIn(object sender, EventArgs args)
        {
            var element = (IGuiElement)sender;
            var i = FElement.IndexOf(element);
            FOnFadedIn[i] = true;
            FOnFadedIn.Stream.IsChanged = true;
        }

        private void Subscribe(IGuiElement element)
        {
            element.OnInteractionBegin += OnInteractionBegin;
            element.OnInteractionEnd += OnInteractionEnd;
            element.OnTouchBegin += OnTouchBegin;
            element.OnTouchEnd += OnTouchEnd;
            element.OnHitBegin += OnHitBegin;
            element.OnHitEnd += OnHitEnd;
            element.OnInteracting += OnInteracting;
            element.OnChildrenAdded += OnChildrenAdded;
            element.OnDeletionStarted += OnDeletionStarted;
            element.OnDeleting += OnDeleting;
            element.OnFadedIn += OnFadedIn;
        }
        private void UnSubscribe(IGuiElement element)
        {
            element.OnInteractionBegin -= OnInteractionBegin;
            element.OnInteractionEnd -= OnInteractionEnd;
            element.OnTouchBegin -= OnTouchBegin;
            element.OnTouchEnd -= OnTouchEnd;
            element.OnHitBegin -= OnHitBegin;
            element.OnHitEnd -= OnHitEnd;
            element.OnInteracting -= OnInteracting;
            element.OnChildrenAdded -= OnChildrenAdded;
            element.OnDeletionStarted -= OnDeletionStarted;
            element.OnDeleting -= OnDeleting;
            element.OnFadedIn -= OnFadedIn;
        }

        public void Evaluate(int SpreadMax)
        {
            if (FElement.SliceCount != PrevElements.SliceCount)
            {
                Outputs?.Clear();
                this.SetSliceCountForAllOutput(FElement.SliceCount, pinSet: Outputs);
                PrevElements.SliceCount = FElement.SliceCount;
            }

            for (int i = 0; i < FElement.SliceCount; i++)
            {
                if (PrevElements[i] == null)
                {
                    Subscribe(FElement[i]);
                }
                else if (PrevElements[i] != FElement[i])
                {
                    UnSubscribe(PrevElements[i]);
                    Subscribe(FElement[i]);
                }

                PrevElements[i] = FElement[i];

                foreach (var output in Outputs)
                {
                    if (!output.IsChanged)
                        output[i] = false;
                }
            }
        }

        public void TurnOn()
        {
            foreach (var element in FElement)
                Subscribe(element);
        }

        public void TurnOff()
        {
            foreach (var element in FElement)
                UnSubscribe(element);
        }
    }
}
