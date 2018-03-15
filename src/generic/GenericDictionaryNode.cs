using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.Nodes.Generic
{
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

        private bool _typeChanged;
        private bool _keysready = false;
        private bool _valsready = false;
        private DiffSpreadPin _defKeys;
        private DiffSpreadPin _defVals;
        private DiffSpreadPin _modKeys;
        private DiffSpreadPin _modVals;
        private DiffSpreadPin _remKeys;
        private DiffSpreadPin _getKeys;
        private SpreadPin _outKeys;
        private SpreadPin _outVals;
        private SpreadPin _queryVals;

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

        private IDictionary dict;

        private void CreatePins()
        {

            _defKeys = _keys.AddInput(new InputAttribute("Default Keys") {Order = 10});
            _modKeys = _keys.AddInput(new InputAttribute("Update Keys") {Order = 13});
            _remKeys = _keys.AddInput(new InputAttribute("Remove Keys") {Order = 17});
            _getKeys = _keys.AddInput(new InputAttribute("Get Keys") {Order = 20});

            _defVals = _vals.AddInput(new InputAttribute("Default Values") {Order = 11});
            _modVals = _vals.AddInput(new InputAttribute("Update Values") {Order = 14});

            _outKeys = _keys.AddOutput(new OutputAttribute("Keys Out") { Order = 0 });
            _outVals = _vals.AddOutput(new OutputAttribute("Values Out") { Order = 1 });
            _queryVals = _vals.AddOutputBinSized(new OutputAttribute("Queried Values") { Order = 2, BinOrder = 3 });
        }

        public void OnImportsSatisfied()
        {
            _keys = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Keys");
            _vals = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Values");
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
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FClear[0]) dict.Clear();
            if (_keysready && _valsready && _typeChanged)
            {
                var dictT = typeof(Dictionary<,>).MakeGenericType(_keys.GroupType, _vals.GroupType);
                dict = (IDictionary)Activator.CreateInstance(dictT);
                _typeChanged = false;
            }

            if (_keysready && _valsready && dict != null)
            {
                if (FResetDefault[0])
                {
                    dict.Clear();
                    if (_defVals.Spread.SliceCount != 0)
                    {
                        for (int i = 0; i < _defKeys.Spread.SliceCount; i++)
                        {
                            if (!dict.Contains(_defKeys[i]))
                                dict.Add(_defKeys[i], _defVals[i]);
                        }
                    }
                }
                if (FSet.IsChanged)
                {
                    if (_modVals.Spread.SliceCount != 0 && _modKeys.Spread.SliceCount != 0)
                    {
                        for (int i = 0; i < _modKeys.Spread.SliceCount; i++)
                        {
                            if (!FSet[i]) continue;

                            if (dict.Contains(_modKeys[i]))
                                dict[_modKeys[i]] = _modVals[i];
                            else dict.Add(_modKeys[i], _modVals[i]);
                        }
                    }
                }
                if (FRemoveAdd.IsChanged)
                {
                    if (_modVals.Spread.SliceCount != 0 && _modKeys.Spread.SliceCount != 0)
                    {
                        for (int i = 0; i < _modKeys.Spread.SliceCount; i++)
                        {
                            if (!FRemoveAdd[i]) continue;

                            if (dict.Contains(_modKeys[i]))
                                dict.Remove(_modKeys[i]);
                            else dict.Add(_modKeys[i], _modVals[i]);
                        }
                    }
                }

                if (FRemove.IsChanged)
                {
                    if (_remKeys.Spread.SliceCount != 0)
                    {
                        for (int i = 0; i < _remKeys.Spread.SliceCount; i++)
                        {
                            if (!FRemove[i]) continue;
                            if (dict.Contains(_remKeys[i])) dict.Remove(_remKeys[i]);
                        }
                    }
                }
                _outVals.Spread.SliceCount = dict.Count;
                _outKeys.Spread.SliceCount = dict.Count;
                int ii = 0;
                foreach (var key in dict.Keys)
                {
                    _outKeys[ii] = key;
                    _outVals[ii] = dict[key];
                    ii++;
                }

                _queryVals.Spread.SliceCount = _getKeys.Spread.SliceCount;
                for (int i = 0; i < _getKeys.Spread.SliceCount; i++)
                {
                    var outspread = (ISpread) _queryVals[i];
                    if (_getKeys[i] == null)
                    {
                        outspread.SliceCount = 0;
                        continue;
                    }
                    if (!dict.Contains(_getKeys[i]))
                    {
                        outspread.SliceCount = 0;
                        continue;
                    }

                    outspread.SliceCount = 1;
                    outspread[0] = dict[_getKeys[i]];
                }
            }
        }
    }
}
