using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Generic
{
    [PluginInfo(
        Name = "Zip",
        Category = "Node",
        Version = "Non-generic",
        Help = "Zips spreads of any type. Only useful for nodes accepting any type.",
        Tags = "join, generic, spreadop",
        AutoEvaluate = true
    )]
    public class NonGenericZipNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IHDEHost Hde;

        private GenericBinSizedInput[] _inputpins = new GenericBinSizedInput[0];

        [Config("Pin Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FPinCount;

        [Output("Output")]
        public ISpread<ISpread<object>> FOut;
        
        protected int CCount = 0;

        public void OnImportsSatisfied()
        {
            FPinCount.Changed += spread => ChangePinCount();
        }

        protected void ChangePinCount()
        {
            var count = FPinCount[0];
            if (FPinCount[0] == 0) return;

            var prev = _inputpins.Take(Math.Min(_inputpins.Length, FPinCount[0])).ToArray();
            _inputpins = new GenericBinSizedInput[FPinCount[0]];
            _inputpins.Fill(prev);

            for (int i = 0; i < _inputpins.Length; i++)
            {
                if (_inputpins[i] == null)
                    _inputpins[i] = new GenericBinSizedInput(FPluginHost, new InputAttribute("Input " + (i+1))
                        {
                            Order = i*2 + 10,
                            BinOrder = i*2 + 11
                        },
                        Hde.MainLoop);
            }
            CCount = count;
        }

        protected int PinsSpreadMax()
        {
            return _inputpins.Max(p => p.SliceCount) * (_inputpins.All(p => p.SliceCount > 0) ? 1 : 0);
        }

        public void Evaluate(int SpreadMax)
        {
            int sprmax = PinsSpreadMax();
            FOut.SliceCount = sprmax * _inputpins.Length;
            int ii = 0;
            if (_inputpins.Length == 0 && FPinCount[0] > 0)
                ChangePinCount();
            
            for (int i = 0; i < sprmax; i++)
            {
                for (int j = 0; j < _inputpins.Length; j++)
                {
                    var inspread = _inputpins[j][i];
                    FOut[ii].SliceCount = inspread?.Count ?? 0;
                    for (int s = 0; s < FOut[ii].SliceCount; s++)
                    {
                        FOut[ii][s] = inspread?[VMath.Zmod(s, inspread.Count)];
                    }
                    ii++;
                }
            }
        }
    }
}
