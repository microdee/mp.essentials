using System;
using System.Collections.Generic;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Hosting.IO;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.Nodes.Generic
{

    public static class TypeUtils
    {
        public static IEnumerable<Type> GetTypes(this Type type)
        {
            // is there any base type?
            if (type == null) yield break;
            yield return type;
            if (type.BaseType == null) yield break;
            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        public static string GetName(this Type T, bool full)
        {
            if (full) return T.FullName;
            else return T.Name;
        }
    }

    [PluginInfo(
        Name = "Cast",
        Category = "Node",
        Author = "microdee",
        AutoEvaluate = true
        )]
    public class NodeCastNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        protected IIOFactory FIOFactory;

        [Import]
        public IPluginHost2 FPluginHost;
        [Import]
        public IHDEHost Hde;

        [Output("Success", Order = 10)]
        public ISpread<bool> FSuccess;

        private GenericInput _input;
        private ConfigurableTypePinGroup _pg;
        private SpreadPin _output;
        private bool _pgready;

        public void OnImportsSatisfied()
        {
            _input = new GenericInput(FPluginHost, new InputAttribute("Input"), Hde.MainLoop);

            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Output", 100);
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                if (_pgready) return;
                _pgready = true;
                _output = _pg.AddOutput(new OutputAttribute("Output"));
            };
        }

        public void Evaluate(int SpreadMax)
        {
            if (!_pgready) return;
            if (!_input.Pin.PinIsChanged) return;
            _output.Spread.SliceCount = FSuccess.SliceCount = _input.Pin.SliceCount;
            for (int i = 0; i < _input.Pin.SliceCount; i++)
            {
                if (_pg.GroupType.IsInstanceOfType(_input[i]))
                {
                    _output[i] = _input[i];
                    FSuccess[i] = true;
                }
                else
                {
                    _output[i] = null;
                    FSuccess[i] = false;
                }
            }
        }
    }


    [PluginInfo(
        Name = "Cast",
        Category = "Node",
        Version = "Binsized",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class NodeCastBinNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        protected IIOFactory FIOFactory;

        [Import]
        public IPluginHost2 FPluginHost;
        [Import]
        public IHDEHost Hde;

        private GenericInput _input;
        private ConfigurableTypePinGroup _pg;
        private SpreadPin _output;
        private bool _pgready;

        public void OnImportsSatisfied()
        {
            _input = new GenericInput(FPluginHost, new InputAttribute("Input"), Hde.MainLoop);

            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Output", 100);
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                if (_pgready) return;
                _pgready = true;
                _output = _pg.AddOutputBinSized(new OutputAttribute("Output"));
            };
        }

        public void Evaluate(int SpreadMax)
        {
            if (!_pgready) return;
            if (!_input.Pin.PinIsChanged) return;
            _output.Spread.SliceCount = _input.Pin.SliceCount;
            for (int i = 0; i < _input.Pin.SliceCount; i++)
            {
                if (_pg.GroupType.IsInstanceOfType(_input[i]))
                {
                    var outspread = (ISpread)_output[i];
                    outspread.SliceCount = 1;
                    outspread[0] = _input[i];
                }
                else
                {
                    var outspread = (ISpread)_output[i];
                    outspread.SliceCount = 0;
                }
            }
        }
    }
}
