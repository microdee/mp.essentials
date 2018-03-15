using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Generic
{
    [PluginInfo(
        Name = "Tuple",
        Category = "Node",
        Version = "Join",
        Author = "microdee",
        AutoEvaluate = true)]
    public class JoinTupleNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import]
        protected IIOFactory FIOFactory;

        [Config("Signature", DefaultString = "")]
        public IDiffSpread<string> FSignature;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FSignature;
            DynamicPins = new PinDictionary(FIOFactory);
        }

        protected override bool IsConfigDefault()
        {
            return FSignature[0] == "";
        }

        protected override void OnConfigPinChanged()
        {
            if(IsConfigDefault()) return;
            DynamicPins.RemoveAllInput();
            DynamicPins.RemoveAllOutput();
            var ptypesnames = FSignature[0].Split('\\');
            var typesnames = ptypesnames.Take(Math.Min(ptypesnames.Length, 7)).ToArray();
            var typelist = typesnames.Select(tn => Type.GetType(tn, true)).ToArray();

            var uglytuplebarebones = new []
            {
                typeof(Tuple<>),
                typeof(Tuple<,>),
                typeof(Tuple<,,>),
                typeof(Tuple<,,,>),
                typeof(Tuple<,,,,>),
                typeof(Tuple<,,,,,>),
                typeof(Tuple<,,,,,,>)
            };

            TupleType = uglytuplebarebones[typesnames.Length-1].MakeGenericType(typelist);
            DynamicPins.AddOutput(TupleType, new OutputAttribute("Output"));
            var item = 0;
            foreach (var t in typelist)
            {
                DynamicPins.AddInput(t, new InputAttribute("Item" + (item+1)));
                item++;
            }
        }

        protected Type TupleType;

        protected PinDictionary DynamicPins;

        public void Evaluate(int SpreadMax)
        {
            if(IsConfigDefault()) return;
            bool changed = DynamicPins.InputPins.Values.Any(pin => pin.Spread.IsChanged);
            var sprmax = DynamicPins.InputPins.Values.Max(pin => pin.Spread.SliceCount);
            if (changed || FSignature.IsChanged)
            {
                DynamicPins.OutputPins["Output"].Spread.SliceCount = sprmax;
                for (int i = 0; i < sprmax; i++)
                {
                    var oarray = new object[DynamicPins.InputPins.Count];
                    for (int j = 0; j < DynamicPins.InputPins.Count; j++)
                    {
                        var pin = DynamicPins.InputPins.Values.ToArray()[j];
                        oarray[j] = pin.Spread[i];
                        pin.Spread.Flush(true);
                    }
                    DynamicPins.OutputPins["Output"].Spread[i] = Activator.CreateInstance(TupleType, oarray);
                }
            }
        }
    }

    [PluginInfo(
        Name = "Tuple",
        Category = "Node",
        Version = "Split",
        Author = "microdee",
        AutoEvaluate = true)]
    public class SplitTupleNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import]
        protected IIOFactory FIOFactory;

        [Config("Signature", DefaultString = "")]
        public IDiffSpread<string> FSignature;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FSignature;
            DynamicPins = new PinDictionary(FIOFactory);
        }

        protected override bool IsConfigDefault()
        {
            return FSignature[0] == "";
        }

        protected override void OnConfigPinChanged()
        {
            if (IsConfigDefault()) return;
            DynamicPins.RemoveAllInput();
            DynamicPins.RemoveAllOutput();
            var ptypesnames = FSignature[0].Split('\\');
            var typesnames = ptypesnames.Take(Math.Min(ptypesnames.Length, 7)).ToArray();
            var typelist = typesnames.Select(tn => Type.GetType(tn, true)).ToArray();

            var uglytuplebarebones = new []
            {
                typeof(Tuple<>),
                typeof(Tuple<,>),
                typeof(Tuple<,,>),
                typeof(Tuple<,,,>),
                typeof(Tuple<,,,,>),
                typeof(Tuple<,,,,,>),
                typeof(Tuple<,,,,,,>)
            };

            TupleType = uglytuplebarebones[typesnames.Length - 1].MakeGenericType(typelist);
            DynamicPins.AddInput(TupleType, new InputAttribute("Input"));
            var item = 0;
            foreach (var t in typelist)
            {
                DynamicPins.AddOutput(t, new OutputAttribute("Item" + (item + 1)));
                item++;
            }
        }

        protected Type TupleType;

        protected PinDictionary DynamicPins;

        public void Evaluate(int SpreadMax)
        {
            if (IsConfigDefault()) return;
            if (DynamicPins.InputPins["Input"].Spread[0] == null) return;
            bool changed = DynamicPins.InputPins["Input"].Spread.IsChanged;
            var sprmax = DynamicPins.InputPins["Input"].Spread.SliceCount;
            if (changed || FSignature.IsChanged)
            {
                foreach (var pin in DynamicPins.OutputPins.Values)
                {
                    pin.Spread.SliceCount = sprmax;
                }
                for (int i = 0; i < sprmax; i++)
                {
                    var tuple = DynamicPins.InputPins["Input"].Spread[i];
                    foreach (var prop in tuple.GetType().GetProperties())
                    {
                        var val = prop.GetValue(tuple);
                        if(DynamicPins.OutputPins.ContainsKey(prop.Name))
                            if (val != null) DynamicPins.OutputPins[prop.Name].Spread[i] = val;
                    }
                }
            }
        }
    }

    [PluginInfo(
        Name = "GetPropertyTypes",
        Category = "Object",
        Author = "microdee",
        Tags = "Tuple",
        AutoEvaluate = true)]
    public class GetSignatureNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<object> FInput;

        [Output("Signature")]
        public ISpread<string> FOutput;

        public void Evaluate(int SpreadMax)
        {
            if(!FInput.IsConnected) return;
            if(!FInput.IsChanged) return;
            FOutput.SliceCount = FInput.SliceCount;
            for (int i = 0; i < FInput.SliceCount; i++)
            {
                if(FInput[i] == null) continue;
                string res = "";
                foreach (var prop in FInput[i].GetType().GetProperties())
                {
                    res += "\\" + prop.PropertyType.AssemblyQualifiedName;
                }
                res = res.Trim('\\');
                FOutput[i] = res;
            }
        }
    }
}
