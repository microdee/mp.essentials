using System;
using System.ComponentModel.Composition;
using System.IO;
using Ceras;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.raw
{
    public static class CerasSerializerHelper
    {
        public static CerasSerializer Serializer()
        {
            var config = new SerializerConfig
            {
                DefaultTargets = TargetMember.AllFields
            };
            
            return new CerasSerializer(config);
        }
    }
    
    [PluginInfo(
        Name = "Serialize",
        Category = "Raw",
        Version = "Ceras",
        Help = "Serialize everything",
        Tags = "generic",
        AutoEvaluate = true
    )]
    public class CerasSerializeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;
        
        private ConfigurableTypePinGroup _pg;
        private bool _typeChanged = false;
        private bool _pgready = false;
        private bool _firstFrame = true;
        private DiffSpreadPin _input;
        private CerasSerializer _ceras = CerasSerializerHelper.Serializer();

        [Output("Output")]
        public ISpread<Stream> Output;
        private Spread<byte[]> _output = new Spread<byte[]>();

        [Output("Success")]
        public ISpread<bool> Success;

        public void OnImportsSatisfied()
        {
            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input");
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if (_pgready) return;
                _pgready = true;

                _input = _pg.AddInput(new InputAttribute("Input") { Order = -1 });
            };
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (!_pgready) return;
            if (_typeChanged || _firstFrame || _input.Spread.IsChanged)
            {
                Success.SliceCount = _input.Spread.SliceCount;
                Output.Resize(_input.Spread.SliceCount, i => new MemoryStream(), stream => stream?.Dispose());
                _output.ResizeAndDismiss(_input.Spread.SliceCount, i => null);
                
                for (int i = 0; i < _input.Spread.SliceCount; i++)
                {
                    Output[i] = Output[i] == null ? new MemoryStream() : Output[i];
                    var obj = _input[i];
                    var buf = _output[i];
                    if (obj == null)
                    {
                        Output[i].SetLength(0);
                        continue;
                    }
                    Output[i].Position = 0;
                    int length = 0;
                    try
                    {
                        length = _ceras.Serialize(obj, ref buf);
                        Success[i] = true;
                    }
                    catch
                    {
                        Success[i] = false;
                        _ceras = CerasSerializerHelper.Serializer();
                        Output[i].SetLength(0);
                        continue;
                    }
                    Output[i].SetLength(length);
                    Output[i].Write(buf, 0, length);
                    _output[i] = buf;
                }
                Output.Stream.IsChanged = true;
            }
            else
            {
                Output.Stream.IsChanged = false;
            }
                
            if (_firstFrame) _firstFrame = false;
            if (_typeChanged) _typeChanged = false;
        }
    }
    
    [PluginInfo(
        Name = "Deserialize",
        Category = "Raw",
        Version = "Ceras",
        Help = "Deserialize everything",
        Tags = "generic",
        AutoEvaluate = true
    )]
    public class CerasDeserializeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory IOFactory;
        [Import] protected IHDEHost Hde;
        
        private ConfigurableTypePinGroup _pg;
        private bool _typeChanged = false;
        private bool _pgready = false;
        private bool _firstFrame = true;
        private SpreadPin _output;
        private CerasSerializer _ceras = CerasSerializerHelper.Serializer();
        
        [Input("Input")]
        public IDiffSpread<Stream> Input;
        private Spread<byte[]> _input = new Spread<byte[]>();

        [Output("Success")]
        public ISpread<bool> Success;

        public void OnImportsSatisfied()
        {
            _pg = new ConfigurableTypePinGroup(FPluginHost, IOFactory, Hde.MainLoop, "Input");
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if (_pgready) return;
                _pgready = true;

                _output = _pg.AddOutput(new OutputAttribute("Output"));
            };
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (!_pgready) return;
            if (_typeChanged || _firstFrame || Input.IsChanged)
            {
                Success.SliceCount = Input.SliceCount;
                _output.Spread.SliceCount = Input.SliceCount;
                _input.ResizeAndDismiss(Input.SliceCount, i => new byte[0]);
                
                for (int i = 0; i < Input.SliceCount; i++)
                {
                    if (Input[i] != null && _input[i].Length < Input[i].Length)
                    {
                        _input[i] = new byte[Input[i].Length];
                    }
                    
                    if (Input[i] == null || Input[i].Read(_input[i], 0, (int)Input[i].Length) <= 0)
                    {
                        _output[i] = null;
                        continue;
                    }

                    try
                    {
                        if (_output[i] == null)
                        {
                            _output[i] = _ceras.Deserialize<object>(_input[i]);
                        }
                        else
                        {
                            var obj = _output[i];
                            _ceras.Deserialize(ref obj, _input[i]);
                            _output[i] = obj;
                        }
                        Success[i] = true;
                    }
                    catch
                    {
                        Success[i] = false;
                        _ceras = CerasSerializerHelper.Serializer();
                        _output[i] = null;
                        continue;
                    }
                }
                _output.SetReflectedChanged(true);
            }
            else
            {
                _output.SetReflectedChanged(false);
            }
                
            if (_firstFrame) _firstFrame = false;
            if (_typeChanged) _typeChanged = false;
        }
    }
}