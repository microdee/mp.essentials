using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interfaces;
using md.stdl.Time;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;

namespace mp.essentials.Nodes.generic
{
    //TODO: version with manual key supply
    [PluginInfo(
        Name = "KeepTemporary",
        Category = "Generic",
        Tags = "",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class GenericKeepTemporaryNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        private class KeptObjectMeta : IMainlooping
        {
            public object Object;
            public readonly StopwatchInteractive Dethklok = new StopwatchInteractive();
            public readonly StopwatchInteractive Age = new StopwatchInteractive();
            public double KeepFor;

            public KeptObjectMeta()
            {
                Age.Start();
            }

            public void Mainloop(float deltatime)
            {
                OnMainLoopBegin?.Invoke(this, EventArgs.Empty);
                Dethklok.Mainloop(deltatime);
                Age.Mainloop(deltatime);
                OnMainLoopEnd?.Invoke(this, EventArgs.Empty);
            }

            public event EventHandler OnMainLoopBegin;
            public event EventHandler OnMainLoopEnd;
        }
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
        private bool _typeChanged;
        private bool _pgready;
        private DiffSpreadPin _input;
        private SpreadPin _output;

        [Input("Keep Time", IsBang = true)]
        public ISpread<double> FKeepTime;
        [Input("Reset", IsBang = true)]
        public ISpread<bool> FReset;

        [Output("Age")]
        public ISpread<double> FAge;
        [Output("Age Stopwatch", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<StopwatchInteractive> FAgeSw;
        [Output("Dethklok")]
        public ISpread<double> FDethklok;
        [Output("Dethklok Stopwatch", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<StopwatchInteractive> FDethklokSw;
        [Output("Dying")]
        public ISpread<bool> FDying;

        private readonly Dictionary<object, KeptObjectMeta> _kept = new Dictionary<object, KeptObjectMeta>();
        private readonly LinkedList<object> _removables = new LinkedList<object>();

        public void OnImportsSatisfied()
        {
            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input");
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if (_pgready) return;
                _pgready = true;

                _input = _pg.AddInput(new InputAttribute("Input") { Order = 10 });
                _output = _pg.AddOutput(new OutputAttribute("Output"));
            };
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (_typeChanged) _typeChanged = false;
            if (!_pgready) return;
            
            if (FReset[0])
            {
                _kept.Clear();
            }

            for (int i = 0; i < _input.Spread.SliceCount; i++)
            {
                if(_input[i] == null) continue;
                if (_kept.ContainsKey(_input[i]))
                {
                    var kometa = _kept[_input[i]];
                    kometa.KeepFor = FKeepTime[i];
                    kometa.Object = _input[i];
                    if (kometa.Dethklok.IsRunning)
                    {
                        kometa.Dethklok.Stop();
                        kometa.Dethklok.Reset();
                        kometa.Dethklok.ResetTriggers();
                    }
                }
                else
                {
                    var kometa = new KeptObjectMeta()
                    {
                        Object = _input[i],
                        KeepFor = FKeepTime[i]
                    };
                    _kept.Add(_input[i], kometa);
                }
            }

            _removables.Clear();
            foreach (var keptObj in _kept.Values)
            {
                keptObj.Mainloop(0.0f);

                var found = false;
                for (int i = 0; i < _input.Spread.SliceCount; i++)
                {
                    if (_input[i] != keptObj.Object) continue;
                    found = true;
                    break;
                }

                if (!found && !keptObj.Dethklok.IsRunning)
                {
                    keptObj.Dethklok.Start();
                }

                if (keptObj.Dethklok.Elapsed.TotalSeconds > keptObj.KeepFor)
                {
                    _removables.AddLast(keptObj.Object);
                }
            }

            foreach (var removable in _removables)
            {
                _kept.Remove(removable);
            }

            _output.Spread.SliceCount =
                FAge.SliceCount = FAgeSw.SliceCount =
                    FDethklok.SliceCount = FDethklokSw.SliceCount =
                        FDying.SliceCount = _kept.Count;

            int ii = 0;
            foreach (var keptObj in _kept.Values)
            {
                _output[ii] = keptObj.Object;
                FAge[ii] = keptObj.Age.Elapsed.TotalSeconds;
                FAgeSw[ii] = keptObj.Age;
                FDethklok[ii] = keptObj.Dethklok.Elapsed.TotalSeconds;
                FDethklokSw[ii] = keptObj.Dethklok;
                FDying[ii] = keptObj.Dethklok.IsRunning;
                ii++;
            }
        }
    }
}
