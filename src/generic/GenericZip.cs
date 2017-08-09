using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V2;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace mp.essentials.Nodes.Generic
{
    [PluginInfo(
        Name = "Zip",
        Category = "Node",
        Help = "Zips spreads of dynamic type.",
        Tags = "join, generic, spreadop"
    )]
    public class GenericZipNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        [Config("Type", DefaultString = "")]
        public IDiffSpread<string> FType;
        [Config("Pin Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FPinCount;

        protected Type CType;
        protected int CCount = 0;
        protected PinDictionary Pd;

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void PreInitialize()
        {
            ConfigPinCopy = FType;
            Pd = new PinDictionary(FIOFactory);
            FPinCount.Changed += spread =>
            {
                if(CType == null) return;
                var count = FPinCount[0];
                if (count > CCount)
                {
                    for(int i=CCount; i<count; i++)
                    {
                        Pd.AddInputBinSized(CType, new InputAttribute("Input " + (i + 1))
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
                        Pd.RemoveInput("Input " + (i + 1));
                    }
                }
                CCount = count;
            };
        }

        protected override void OnConfigPinChanged()
        {
            if (CType != null)
            {
                Pd.RemoveAllInput();
                Pd.RemoveAllOutput();
            }
            var t = Type.GetType(FType[0]);
            if (t == null) return;
            CType = t;
            Pd.AddOutputBinSized(CType, new OutputAttribute("Output"));
            for (int i = 0; i < FPinCount[0]; i++)
            {
                Pd.AddInputBinSized(CType, new InputAttribute("Input " + (i + 1))
                {
                    Order = i * 2,
                    BinOrder = i * 2 + 1
                });
            }
            CCount = FPinCount[0];
        }

        public void Evaluate(int SpreadMax)
        {
            if(CType == null) return;
            var outpin = Pd.OutputPins["Output"];
            outpin.Spread.SliceCount = Pd.InputSpreadMax * Pd.InputPins.Count;
            int ii = 0;
            for (int i = 0; i < Pd.InputSpreadMax; i++)
            {
                for (int j = 0; j < Pd.InputPins.Count; j++)
                {
                    var cspread = (NGISpread)outpin.Spread[ii];
                    var inspread = (NGISpread)Pd.InputPins["Input " + (j + 1)].Spread[i];
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
        Tags = "join, generic, spreadop"
    )]
    public class GenericUnzipNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        [Config("Type", DefaultString = "")]
        public IDiffSpread<string> FType;
        [Config("Pin Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FPinCount;

        protected Type CType;
        protected int CCount = 0;
        protected PinDictionary Pd;

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void PreInitialize()
        {
            ConfigPinCopy = FType;
            Pd = new PinDictionary(FIOFactory);
            FPinCount.Changed += spread =>
            {
                if (CType == null) return;
                var count = FPinCount[0];
                if (count > CCount)
                {
                    for (int i = CCount; i < count; i++)
                    {
                        Pd.AddOutputBinSized(CType, new OutputAttribute("Output " + (i + 1))
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
                        Pd.RemoveOutput("Output " + (i + 1));
                    }
                }
                CCount = count;
            };
        }

        protected override void OnConfigPinChanged()
        {
            if (CType != null)
            {
                Pd.RemoveAllInput();
                Pd.RemoveAllOutput();
            }
            var t = Type.GetType(FType[0]);
            if (t == null) return;
            CType = t;
            Pd.AddInputBinSized(CType, new InputAttribute("Input"));
            for (int i = 0; i < FPinCount[0]; i++)
            {
                Pd.AddOutputBinSized(CType, new OutputAttribute("Output " + (i + 1))
                {
                    Order = i * 2,
                    BinOrder = i * 2 + 1
                });
            }
            CCount = FPinCount[0];
        }

        public void Evaluate(int SpreadMax)
        {
            if (CType == null) return;
            var inpin = Pd.InputPins["Input"];
            for (int i = 0; i < Pd.OutputPins.Count; i++)
            {
                Pd.OutputPins["Output " + (i + 1)].Spread.SliceCount = (int)Math.Ceiling((double)inpin.Spread.SliceCount / Pd.OutputPins.Count);
            }
            for (int i = 0; i < Math.Max(inpin.Spread.SliceCount, Pd.OutputPins.Count); i++)
            {
                var o = i % Pd.OutputPins.Count;
                var ii = (int)Math.Floor((double) i / Pd.OutputPins.Count);
                var inspread = (NGISpread)inpin.Spread[i];
                var outspread = (NGISpread)Pd.OutputPins["Output " + (o + 1)].Spread[ii];
                outspread.SliceCount = inspread.SliceCount;
                for (int s = 0; s < outspread.SliceCount; s++)
                {
                    outspread[s] = inspread[s];
                }
            }
        }
    }
}
