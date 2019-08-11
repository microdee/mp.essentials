using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using mp.pddn;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.Nodes.Generic
{

    [PluginInfo(
        Name = "Count",
        Category = "Dictionary",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class DictionaryCountNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private GenericInput _input;

        [Output("Count")]
        public ISpread<int> CountOut;

        private int _dictChangeCounter = -1;

        public void OnImportsSatisfied()
        {
            _input = new GenericInput(FPluginHost, new InputAttribute("Dictionary Input"), Hde.MainLoop);
        }

        public void Evaluate(int SpreadMax)
        {
            CountOut.SliceCount = _input.Pin.SliceCount;
            for (int i = 0; i < _input.Pin.SliceCount; i++)
            {
                CountOut[i] = _input[i] is IDictionary dict ? dict.Count : 0;
            }
        }

    }

    [PluginInfo(
        Name = "Dictionary",
        Category = "Generic",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class GenericDictionaryNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _keys;
        private ConfigurableTypePinGroup _vals;
        private PinDictionary _pd;
        private Type _dictType;
        private Type _inoutDictType;

        private MethodInfo _openCast;
        private MethodInfo _keyCast;
        private MethodInfo _valCast;

        private static T Cast<T>(dynamic entity) => (T)entity;

        private bool _typeChanged;
        private bool _keysready = false;
        private bool _valsready = false;
        private bool _dictinUsed = false;
        private DiffSpreadPin _defKeys;
        private DiffSpreadPin _defVals;
        private DiffSpreadPin _modKeys;
        private DiffSpreadPin _modVals;
        private DiffSpreadPin _remKeys;
        private DiffSpreadPin _getKeys;
        private SpreadPin _outKeys;
        private SpreadPin _queryVals;
        private DiffSpreadPin _dictin;
        private SpreadPin _dictout;

        [Input("Reset to Default", IsBang = true, Order = 12)]
        public ISpread<bool> FResetDefault;
        [Input("Update or Add", IsBang = true, Order = 15)]
        public ISpread<bool> FSet;
        [Input("Remove or Add", IsBang = true, Order = 16)]
        public ISpread<bool> FRemoveAdd;
        [Input("Remove", IsBang = true, Order = 18)]
        public ISpread<bool> FRemove;
        [Input("Clear", IsBang = true, Order = 19)]
        public ISpread<bool> FClear;
        [Input("Get All Values", Order = 21)]
        public IDiffSpread<bool> FGetAll;

        [Output(
            "Changed",
            IsBang = true,
            Visibility = PinVisibility.Hidden,
            Order = 10000
        )]
        public ISpread<bool> FChanged;

        private dynamic _dict;
        private int _dictChangeCounter = -1;

        private void CreatePins()
        {

            _defKeys = _keys.AddInput(new InputAttribute("Default Keys") {Order = 10});
            _modKeys = _keys.AddInput(new InputAttribute("Update Keys") {Order = 13});
            _remKeys = _keys.AddInput(new InputAttribute("Remove Keys") {Order = 17});
            _getKeys = _keys.AddInput(new InputAttribute("Get Keys") {Order = 20});

            _defVals = _vals.AddInput(new InputAttribute("Default Values") {Order = 11});
            _modVals = _vals.AddInput(new InputAttribute("Update Values") {Order = 14});

            _outKeys = _keys.AddOutput(new OutputAttribute("Keys") { Order = 2 });
            _queryVals = _vals.AddOutputBinSized(new OutputAttribute("Values") { Order = 3, BinOrder = 4 });
            _dictType = typeof(Dictionary<,>).MakeGenericType(_keys.GroupType, _vals.GroupType);
            _inoutDictType = typeof(IDictionary<,>).MakeGenericType(_keys.GroupType, _vals.GroupType);
            _pd.RemoveInput("Dictionary In");
            _pd.RemoveOutput("Dictionary Out");
            _dictin = _pd.AddInput(_inoutDictType, new InputAttribute("Dictionary In"));
            _dictout = _pd.AddOutput(_inoutDictType, new OutputAttribute("Dictionary Out") { Order = 1 });
        }

        public void OnImportsSatisfied()
        {
            _keys = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Keys");
            _vals = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Values");
            _pd = new PinDictionary(FIOFactory);
            _keys.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if (_keysready) return;
                _keysready = true;
                if(!(_keysready && _valsready)) return;
                CreatePins();
            };
            _vals.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if (_valsready) return;
                _valsready = true;
                if (!(_keysready && _valsready)) return;
                CreatePins();

            };
        }

        private void PrepareDict(bool perframe)
        {
            var dictinValid = _dictin.Spread.SliceCount > 0 && _inoutDictType.IsInstanceOfType(_dictin[0]);
            if (perframe)
            {

                if (dictinValid && !_dictinUsed)
                {
                    _dict = _dictin[0];
                    _dictinUsed = true;
                }

                if (!dictinValid && _dictinUsed)
                {
                    _dict = Activator.CreateInstance(_dictType);
                    _dictinUsed = false;
                }
            }
            else
            {
                if (dictinValid)
                {
                    _dict = _dictin[0];
                    _dictinUsed = true;
                }
                else
                {
                    _dict = Activator.CreateInstance(_dictType);
                    _dictinUsed = false;
                }
            }
        }

        private void Clear()
        {
            if(!FClear[0]) return;
            _dict.Clear();
            ((object) _dict).NotifyChange();
        }

        private void CreateKeyCastDelegate()
        {
            _openCast = this.GetType().GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic);
            _keyCast = _openCast.MakeGenericMethod(_keys.GroupType);
        }
        private void CreateValCastDelegate()
        {
            _openCast = this.GetType().GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic);
            _valCast = _openCast.MakeGenericMethod(_vals.GroupType);
        }

        private dynamic GetKey(int i, DiffSpreadPin keys)
        {
            if(_keyCast == null) CreateKeyCastDelegate();
            return _keyCast.Invoke(this, new []{ keys[i] });
        }

        private dynamic GetValue(int i, DiffSpreadPin vals)
        {
            if (_valCast == null) CreateValCastDelegate();
            return _valCast.Invoke(this, new[] { vals[i] });
        }

        private void ResetDefault()
        {
            if(!FResetDefault[0]) return;
            _dict.Clear();
            if (_defVals.Spread.SliceCount != 0)
            {
                for (int i = 0; i < _defKeys.Spread.SliceCount; i++)
                {
                    if (!_dict.ContainsKey(GetKey(i, _defKeys)))
                        _dict.Add(GetKey(i, _defKeys), GetValue(i, _defVals));
                }
            }
            ((object)_dict).NotifyChange();
        }

        private void Set()
        {
            if(!FSet.IsChanged) return;
            if (_modVals.Spread.SliceCount != 0 && _modKeys.Spread.SliceCount != 0)
            {
                for (int i = 0; i < _modKeys.Spread.SliceCount; i++)
                {
                    if (!FSet[i]) continue;

                    if (_dict.ContainsKey(GetKey(i, _modKeys)))
                        _dict[GetKey(i, _modKeys)] = GetValue(i, _modVals);
                    else _dict.Add(GetKey(i, _modKeys), GetValue(i, _modVals));
                    ((object)_dict).NotifyChange();
                }
            }
        }

        private void RemoveAdd()
        {
            if (!FRemoveAdd.IsChanged) return;
            if (_modVals.Spread.SliceCount != 0 && _modKeys.Spread.SliceCount != 0)
            {
                for (int i = 0; i < _modKeys.Spread.SliceCount; i++)
                {
                    if (!FRemoveAdd[i]) continue;

                    if (_dict.ContainsKey(GetKey(i, _modKeys)))
                        _dict.Remove(GetKey(i, _modKeys));
                    else _dict.Add(GetKey(i, _modKeys), GetValue(i, _modVals));
                    ((object)_dict).NotifyChange();
                }
            }
        }

        private void Remove()
        {
            if (!FRemove.IsChanged) return;
            if (_remKeys.Spread.SliceCount != 0)
            {
                for (int i = 0; i < _remKeys.Spread.SliceCount; i++)
                {
                    if (!FRemove[i]) continue;
                    if (_dict.ContainsKey(GetKey(i, _remKeys))) _dict.Remove(GetKey(i, _remKeys));
                    ((object)_dict).NotifyChange();
                }
            }
        }

        private void Get()
        {
            if (_dictin.Spread.IsChanged && _dictin.Spread.SliceCount > 0)
                ((object)_dict).NotifyChange();

            var changed = ((object)_dict).CheckChanged(ref _dictChangeCounter);
            FChanged[0] = changed;

            if (FGetAll[0])
            {
                if(changed || FGetAll.IsChanged)
                {
                    _queryVals.Spread.SliceCount = _dict.Count;
                    _outKeys.Spread.SliceCount = _dict.Count;
                    int ii = 0;
                    foreach (var key in _dict.Keys)
                    {
                        var outspread = (ISpread) _queryVals[ii];
                        outspread.SliceCount = 1;
                        _outKeys[ii] = key;
                        outspread[0] = _dict[key];
                        ii++;
                    }
                }
            }
            else
            {
                if(changed || _getKeys.Spread.IsChanged || FGetAll.IsChanged)
                {
                    _queryVals.Spread.SliceCount = _outKeys.Spread.SliceCount = _getKeys.Spread.SliceCount;
                    for (int i = 0; i < _getKeys.Spread.SliceCount; i++)
                    {
                        var outspread = (ISpread) _queryVals[i];
                        if (_getKeys[i] == null)
                        {
                            outspread.SliceCount = 0;
                            continue;
                        }

                        if (!_dict.ContainsKey(GetKey(i, _getKeys)))
                        {
                            outspread.SliceCount = 0;
                            continue;
                        }

                        outspread.SliceCount = 1;
                        _outKeys[i] = _getKeys[i];
                        outspread[0] = _dict[GetKey(i, _getKeys)];
                    }
                }
            }
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (_keysready && _valsready && _typeChanged)
            {
                PrepareDict(false);
                _typeChanged = false;
                ((object)_dict).NotifyChange();
            }

            if (_keysready && _valsready && _dict != null)
            {
                PrepareDict(true);

                Clear();
                ResetDefault();
                Set();
                RemoveAdd();
                Remove();
                Get();

                var changed = FChanged[0];

                _dictout.SetReflectedChanged(false);
                if (_dictout[0]?.GetHashCode() != _dict?.GetHashCode())
                {
                    _dictout.SetReflectedChanged(true);
                    _dictout[0] = _dict;
                    changed = true;
                }

                _outKeys.SetReflectedChanged(false);
                _queryVals.SetReflectedChanged(false);

                if (changed || _getKeys.Spread.IsChanged || FGetAll.IsChanged)
                {
                    _outKeys.SetReflectedChanged(true);
                    _queryVals.SetReflectedChanged(true);
                }
            }
        }
    }
}

