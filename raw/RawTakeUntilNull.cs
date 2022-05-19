using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.raw
{
    [PluginInfo(
        Name = "TakeUntilNull",
        Category = "Raw",
        Author = "microdee",
        Help = "Copies a raw stream over until the first 0 byte"
        )]
    public class RawTakeUntilNull : IPluginEvaluate
    {
        [Input("Input")]
        public IDiffSpread<Stream> FInput;
        [Input("Null Count")]
        public IDiffSpread<int> NullCount;

        [Output("Output")]
        public ISpread<Stream> FOutput;
        [Output("Data Length")]
        public ISpread<int> FDataLength;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsChanged)
            {
                FOutput.ResizeAndDispose(FInput.SliceCount, () => new MemoryComStream());
                FDataLength.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if (FOutput[i] == null) FOutput[i] = new MemoryComStream();
                    if (FInput[i].Length < FOutput[i].Length) FOutput[i].SetLength(FInput[i].Length);
                    FInput[i].Position = 0;
                    FOutput[i].Position = 0;
                    int dl = 0;
                    int desiredNullCount = NullCount[i];
                    int nullCount = 0;
                    while (true)
                    {
                        int cbyte = FInput[i].ReadByte();
                        if (cbyte < 0)
                        {
                            desiredNullCount = 0;
                            break;
                        }
                        if (cbyte == 0)
                        {
                            nullCount++;
                            if (nullCount >= NullCount[i])
                            {
                                dl++;
                                break;
                            }
                        }
                        else
                        {
                            nullCount = 0;
                        }
                        FOutput[i].WriteByte((byte) cbyte);
                        dl++;
                    }
                    dl = dl - desiredNullCount;
                    FOutput[i].SetLength(dl);
                    FDataLength[i] = dl;
                    FInput[i].Position = 0;
                    FOutput[i].Position = 0;
                    FOutput.Flush(true);
                }
            }
        }
    }
}
