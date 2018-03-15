using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.Nodes.Generic
{
    [PluginInfo(
        Name = "Zip",
        Category = "Node",
        Help = "Zips spreads of dynamic type.",
        Tags = "join, generic, spreadop",
        AutoEvaluate = true
    )]
    public class GenericZipNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
        private SpreadPin _output;
        private bool _pgready;

        [Config("Pin Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FPinCount;
        
        protected int CCount = 0;

        public void OnImportsSatisfied()
        {
            FPinCount.Changed += spread => ChangePinCount();
            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input", 100);
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                if (_pgready) return;
                _pgready = true;
                _output = _pg.AddOutputBinSized(new OutputAttribute("Output"));
                ChangePinCount();
            };
        }

        protected void ChangePinCount()
        {
            var count = FPinCount[0];
            if (!_pgready) return;

            if (count > CCount)
            {
                for (int i = CCount; i < count; i++)
                {
                    _pg.AddInputBinSized(new InputAttribute("Input " + (i + 1))
                    {
                        Order = i * 2,
                        BinOrder = i * 2 + 1
                    });
                }
            }
            else if (count < CCount)
            {
                for (int i = count; i < CCount; i++)
                {
                    _pg.RemoveInput("Input " + (i + 1));
                }
            }
            CCount = count;
        }

        public void Evaluate(int SpreadMax)
        {
            if(!_pgready) return;
            _output.Spread.SliceCount = _pg.Pd.InputSpreadMax * _pg.Pd.InputPins.Count;
            int ii = 0;
            if (_pg.Pd.InputPins.Count == 0 && FPinCount[0] > 0)
                ChangePinCount();
            for (int i = 0; i < _pg.Pd.InputSpreadMax; i++)
            {
                for (int j = 0; j < _pg.Pd.InputPins.Count; j++)
                {
                    var cspread = (ISpread) _output[ii];
                    var inspread = (ISpread)_pg.Pd.InputPins["Input " + (j + 1)][i];
                    cspread.SliceCount = inspread.SliceCount;
                    for (int s = 0; s < inspread.SliceCount; s++)
                    {
                        cspread[s] = inspread[s];
                    }
                    ii++;
                }
            }
        }
    }

    [PluginInfo(
        Name = "Unzip",
        Category = "Node",
        Help = "Unzips spreads of dynamic type.",
        Tags = "join, generic, spreadop",
        AutoEvaluate = true
    )]
    public class GenericUnzipNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
        private DiffSpreadPin _input;
        private bool _pgready;

        [Config("Pin Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FPinCount;
        
        protected int CCount = 0;

        public void OnImportsSatisfied()
        {
            FPinCount.Changed += spread => ChangePinCount();
            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Output", 100);
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                if (_pgready) return;
                _pgready = true;
                _input = _pg.AddInputBinSized(new InputAttribute("Input"));
                ChangePinCount();
            };
        }

        protected void ChangePinCount()
        {
            var count = FPinCount[0];
            if (!_pgready) return;

            if (count > CCount)
            {
                for (int i = CCount; i < count; i++)
                {
                    _pg.AddOutputBinSized(new OutputAttribute("Output " + (i + 1))
                    {
                        Order = i * 2,
                        BinOrder = i * 2 + 1
                    });
                }
            }
            else if (count < CCount)
            {
                for (int i = count; i < CCount; i++)
                {
                    _pg.RemoveOutput("Output " + (i + 1));
                }
            }
            CCount = count;
        }

        public void Evaluate(int SpreadMax)
        {
            if (!_pgready) return;
            for (int i = 0; i < _pg.Pd.OutputPins.Count; i++)
            {
                _pg.Pd.OutputPins["Output " + (i + 1)].Spread.SliceCount =
                    (int)Math.Ceiling((double)_input.Spread.SliceCount / _pg.Pd.OutputPins.Count);
            }
            if(_pg.Pd.OutputPins.Count == 0 && FPinCount[0] > 0)
                ChangePinCount();
            for (int i = 0; i < Math.Max(_input.Spread.SliceCount, _pg.Pd.OutputPins.Count); i++)
            {
                var o = i % _pg.Pd.OutputPins.Count;
                var ii = (int)Math.Floor((double) i / _pg.Pd.OutputPins.Count);
                var inspread = (ISpread) _input[i];
                var outspread = (ISpread) _pg.Pd.OutputPins["Output " + (o + 1)][ii];
                outspread.SliceCount = inspread.SliceCount;
                for (int s = 0; s < outspread.SliceCount; s++)
                {
                    outspread[s] = inspread[s];
                }
            }
        }
    }
}
