using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace mp.essentials.Nodes.Messages
{
    [PluginInfo(
        Name = "MessagePath",
        Category = "Message",
        Author = "microdee",
        Help = "Query nested messages in XPath fashion. Syntax is Field|NestedField|`RegexMatchFields`"
        )]
    public class MessagePathNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import]
        public IIOFactory FIOFactory;

        [Config("Expected Type")]
        public IDiffSpread<string> FExpectedType;

        [Input("Message")]
        public Pin<Message> FMessage;
        [Input("Path")]
        public IDiffSpread<string> FPath;
        [Input("Separator", DefaultString = "|", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<string> FSep;

        [Output("Source")]
        public ISpread<ISpread<Message>> FSource;
        [Output("Field")]
        public ISpread<ISpread<string>> FField;

        protected PinDictionary pd;
        protected Spread<Spread<ObjectMessagePair>> Result = new Spread<Spread<ObjectMessagePair>>();
        protected Spread<int> PrevHashes = new Spread<int>();
        protected int PrevMessageSlicecount = 0;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FExpectedType;
            pd = new PinDictionary(FIOFactory);
        }

        protected override void OnConfigPinChanged()
        {
            bool typevalid = false;
            Type expectedtype = null;
            foreach (var tr in TypeIdentity.Instance.Values)
            {
                if (tr.Alias.Equals(FExpectedType[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    typevalid = true;
                    expectedtype = tr.Type;
                }
            }
            if(!typevalid) return;
            if(pd.OutputPins.ContainsKey("Output"))
                if(pd.OutputPins["Output"].Type == expectedtype) return;
            pd.RemoveAllOutput();
            pd.AddOutputBinSized(expectedtype, new OutputAttribute("Output"));
            GetData();
        }

        protected void GetData()
        {
            var outspread = pd.OutputPins["Output"].Spread;
            outspread.SliceCount = Math.Max(FMessage.SliceCount, FPath.SliceCount);
            Result.SliceCount = outspread.SliceCount;
            FSource.SliceCount = outspread.SliceCount;
            FField.SliceCount = outspread.SliceCount;
            for (int i = 0; i < outspread.SliceCount; i++)
            {
                if(Result[i] == null) Result[i] = new Spread<ObjectMessagePair>();
                var cspread = (NGISpread) outspread[i];
                cspread.SliceCount = 0;
                Result[i].SliceCount = 0;
                FSource[i].SliceCount = 0;
                FField[i].SliceCount = 0;
                var objmsgpairs = FMessage[i].MPath(FPath[i], FSep[i]);
                foreach (var omp in objmsgpairs)
                {
                    Result[i].Add(omp);
                    cspread.SliceCount++;
                    cspread[-1] = omp.Object;
                    FSource[i].Add(omp.Source);
                    FField[i].Add(omp.Field);
                }
            }
        }

        protected override bool IsConfigDefault()
        {
            return FExpectedType[0] == "";
        }

        public void Evaluate(int SpreadMax)
        {
            if(!FMessage.IsConnected) return;
            if(FPath.IsChanged) GetData();

            if(!pd.OutputPins.ContainsKey("Output")) return;

            var outspread = pd.OutputPins["Output"].Spread;
            if (FMessage.SliceCount != PrevMessageSlicecount)
                GetData();
            else
            {
                if (FMessage.IsChanged)
                {
                    bool traversingneeded = false;
                    for (int i = 0; i < Result.SliceCount; i++)
                    {
                        if (FMessage[i].GetHashCode() != PrevHashes[i])
                        {
                            traversingneeded = true;
                            break;
                        }
                        var cspread = (NGISpread) outspread[i];
                        for (int j = 0; j < Result[i].SliceCount; j++)
                        {
                            var uptodateobj = Result[i][j].Source[Result[i][j].Field][Result[i][j].Index];
                            cspread[j] = uptodateobj;
                            Result[i][j].Object = uptodateobj;
                        }
                    }
                    if (traversingneeded) GetData();
                }
            }

            PrevHashes.SliceCount = FMessage.SliceCount;
            for (int i = 0; i < FMessage.SliceCount; i++)
                PrevHashes[i] = FMessage[i].GetHashCode();
            PrevMessageSlicecount = FMessage.SliceCount;
        }
    }
}
