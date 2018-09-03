using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;
using md.stdl.Coding;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.generic
{
    [PluginInfo(
        Name = "Serialize",
        Category = "XAML",
        Help = "Serialize clr objects into XAML",
        AutoEvaluate = true
    )]
    public class XamlSerializerNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private GenericInput _input;

        [Input("Serialize", IsBang = true, Order = 10)]
        public ISpread<bool> FSet;

        [Output("Output")]
        public ISpread<string> FOutput;
        [Output("Error")]
        public ISpread<string> FError;

        public void OnImportsSatisfied()
        {
            _input = new GenericInput(FPluginHost, new InputAttribute("Input"), Hde.MainLoop);
        }

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = FError.SliceCount = _input.Pin.SliceCount;
            for (int i = 0; i < _input.Pin.SliceCount; i++)
            {
                if (FSet[i])
                {
                    try
                    {
                        FOutput[i] = XamlServices.Save(_input[i]);
                        FError[i] = "";
                    }
                    catch (Exception e)
                    {
                        FError[i] = e.Message;
                    }
                }
            }
        }
    }

    [PluginInfo(
        Name = "Deserialize",
        Category = "XAML",
        Help = "DeSerialize XAML string into respective clr object",
        AutoEvaluate = true
    )]
    public class XamlDeserializeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;
        
        [Input("Input")]
        public ISpread<string> FInput;
        [Input("Deserialize", IsBang = true)]
        public ISpread<bool> FSet;

        [Output("Success", Order = 10)]
        public ISpread<bool> FSuccess;
        [Output("Error", Order = 11)]
        public ISpread<string> FError;

        private ConfigurableTypePinGroup _pg;
        private SpreadPin _output;
        private bool _pgready;

        public void OnImportsSatisfied()
        {
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
            _output.Spread.SliceCount = FSuccess.SliceCount = FError.SliceCount = FInput.SliceCount;
            for (int i = 0; i < FInput.SliceCount; i++)
            {
                if (FSet[i])
                {
                    try
                    {
                        var res = XamlServices.Parse(FInput[i]);
                        if (_pg.GroupType.IsInstanceOfType(res))
                        {
                            _output[i] = res;
                            FSuccess[i] = true;
                        }
                        else
                        {
                            FSuccess[i] = false;
                        }
                        FError[i] = "";
                    }
                    catch (Exception e)
                    {
                        FSuccess[i] = false;
                        FError[i] = e.Message;
                    }
                }
            }
        }
    }
}
