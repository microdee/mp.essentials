using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.notui
{
    public class ElementEventFlattener
    {
        public bool OnInteractionBegin { get; private set; }
        public bool OnInteractionEnd { get; private set; }
        public bool OnTouchBegin { get; private set; }
        public bool OnTouchEnd { get; private set; }
        public bool OnHitBegin { get; private set; }
        public bool OnHitEnd { get; private set; }
        public bool OnInteracting { get; private set; }
        public bool OnChildrenUpdated { get; private set; }
        public bool OnDeletionStarted { get; private set; }
        public bool OnDeleting { get; private set; }
        public bool OnFadedIn { get; private set; }

        private void _OnInteractionBeginHandler(object sender, TouchInteractionEventArgs args) => OnInteractionBegin = true;
        private void _OnInteractionEndHandler(object sender, TouchInteractionEventArgs args) => OnInteractionEnd = true;
        private void _OnTouchBeginHandler(object sender, TouchInteractionEventArgs args) => OnTouchBegin = true;
        private void _OnTouchEndHandler(object sender, TouchInteractionEventArgs args) => OnTouchEnd = true;
        private void _OnHitBeginHandler(object sender, TouchInteractionEventArgs args) => OnHitBegin = true;
        private void _OnHitEndHandler(object sender, TouchInteractionEventArgs args) => OnHitEnd = true;
        private void _OnInteractingHandler(object sender, EventArgs args) => OnInteracting = true;
        private void _OnChildrenUpdatedHandler(object sender, ChildrenUpdatedEventArgs args) => OnChildrenUpdated = true;
        private void _OnDeletionStartedHandler(object sender, EventArgs args) => OnDeletionStarted = true;
        private void _OnDeletingHandler(object sender, EventArgs args) => OnDeleting = true;
        private void _OnFadedInHandler(object sender, EventArgs args) => OnFadedIn = true;

        public void Subscribe(NotuiElement element)
        {
            try
            {
                element.OnInteractionBegin -= _OnInteractionBeginHandler;
                element.OnInteractionEnd -= _OnInteractionEndHandler;
                element.OnTouchBegin -= _OnTouchBeginHandler;
                element.OnTouchEnd -= _OnTouchEndHandler;
                element.OnHitBegin -= _OnHitBeginHandler;
                element.OnHitEnd -= _OnHitEndHandler;
                element.OnInteracting -= _OnInteractingHandler;
                element.OnChildrenUpdated -= _OnChildrenUpdatedHandler;
                element.OnDeletionStarted -= _OnDeletionStartedHandler;
                element.OnDeleting -= _OnDeletingHandler;
                element.OnFadedIn -= _OnFadedInHandler;
            } catch { }

            element.OnInteractionBegin += _OnInteractionBeginHandler;
            element.OnInteractionEnd += _OnInteractionEndHandler;
            element.OnTouchBegin += _OnTouchBeginHandler;
            element.OnTouchEnd += _OnTouchEndHandler;
            element.OnHitBegin += _OnHitBeginHandler;
            element.OnHitEnd += _OnHitEndHandler;
            element.OnInteracting += _OnInteractingHandler;
            element.OnChildrenUpdated += _OnChildrenUpdatedHandler;
            element.OnDeletionStarted += _OnDeletionStartedHandler;
            element.OnDeleting += _OnDeletingHandler;
            element.OnFadedIn += _OnFadedInHandler;
        }

        public void Reset()
        {
            OnInteractionBegin = false;
            OnInteractionEnd = false;
            OnTouchBegin = false;
            OnTouchEnd = false;
            OnHitBegin = false;
            OnHitEnd = false;
            OnInteracting = false;
            OnChildrenUpdated = false;
            OnDeletionStarted = false;
            OnDeleting = false;
            OnFadedIn = false;
        }

        private IHDEHost _hdeHost;

        public ElementEventFlattener(IHDEHost host)
        {
            _hdeHost = host;
            _hdeHost.MainLoop.OnResetCache += (sender, args) => Reset();
        }
    }

    public class VEnvironmentData : AuxiliaryObject
    {
        public Dictionary<string, object> NodeSpecific { get; set; } = new Dictionary<string, object>();
        public ElementEventFlattener FlattenedEvents;

        public override AuxiliaryObject Copy()
        {
            return new VEnvironmentData();
        }

        public override void UpdateFrom(AuxiliaryObject other)
        {
            if (other is VEnvironmentData venvdat)
            {
                NodeSpecific = venvdat.NodeSpecific;
            }
        }
    }

    public static class NotuiUtils
    {
        public static void AttachManagementObject(this NotuiElement element, string nodepath, object obj)
        {
            if (element.EnvironmentObject == null)
                element.EnvironmentObject = new VEnvironmentData();
            if (element.EnvironmentObject is VEnvironmentData venvdat)
            {
                if (venvdat.NodeSpecific.ContainsKey(nodepath))
                {
                    venvdat.NodeSpecific[nodepath] = obj;
                }
                else
                {
                    venvdat.NodeSpecific.Add(nodepath, obj);
                }
            }
        }

        public static void AttachSliceId(this NotuiElement element, string nodepath, int id)
        {
            element.AttachManagementObject(nodepath, id);
        }
    }
}
