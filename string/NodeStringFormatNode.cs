using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes
{
    [PluginInfo(
        Name = "Format",
        Category = "String",
        Version = "Node",
        Help = "Format version where input pins can be any object",
        Author = "microdee"
    )]
    public class NodeStringFormatNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] public IPluginHost2 Host;
        [Import] public IHDEHost Hde;

        [Config("Pin Count", DefaultValue = 1)]
        public IDiffSpread<int> FPinCount;

        [Input("Input")]
        public IDiffSpread<string> FIn;
        [Input("Culture", Visibility = PinVisibility.Hidden)]
        public IDiffSpread<string> FCulture;

        [Output("Output")]
        public ISpread<string> FOut;

        private GenericInput[] _objs = new GenericInput[0];

        public void OnImportsSatisfied()
        {
            FPinCount.Changed += spread =>
            {
                if (FPinCount[0] == 0) return;

                var prev = _objs.Take(Math.Min(_objs.Length, FPinCount[0])).ToArray();
                _objs = new GenericInput[FPinCount[0]];
                _objs.Fill(prev);

                for (int i = 0; i < _objs.Length; i++)
                {
                    if (_objs[i] == null)
                        _objs[i] = new GenericInput(Host, new InputAttribute("Input " + i) {Order = i + 10},
                            Hde.MainLoop);
                }
            };
        }

        public void Evaluate(int SpreadMax)
        {
            var sprmax = FOut.SliceCount = Math.Max(FIn.SliceCount, _objs.Max(p => p.Pin.SliceCount));
            for (int i = 0; i < sprmax; i++)
            {
                var culture = string.IsNullOrWhiteSpace(FCulture[i])
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.GetCultureInfo(FCulture[i]);
                FOut[i] = string.Format(culture, FIn[i], _objs.Select(p => p[i]).ToArray());
            }
        }
    }
}
