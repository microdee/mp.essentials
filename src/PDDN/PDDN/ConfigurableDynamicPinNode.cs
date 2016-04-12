using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.PDDN
{
    public abstract class ConfigurableDynamicPinNode<TConfigType> : IPartImportsSatisfiedNotification
    {
        protected IDiffSpread<TConfigType> ConfigPinCopy;

        protected virtual void PreInitialize() { }
        protected virtual void Initialize() { }
        protected virtual void OnConfigPinChanged() { }
        protected bool Initialized = false;

        protected virtual bool IsConfigDefault()
        {
            return false;
        }

        public void OnImportsSatisfied()
        {
            PreInitialize();
            ConfigPinCopy.Changed += _OnConfigPinChanged;
        }
        private void _OnConfigPinChanged(IDiffSpread<TConfigType> spread)
        {
            OnConfigPinChanged();
            if (Initialized) return;
            if (IsConfigDefault()) return;
            Initialize();
            Initialized = true;
        }
    }
}
