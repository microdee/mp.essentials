using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.raw
{
    [PluginInfo(
        Name = "Reader",
        Category = "Raw",
        Version = "SharedMemory.Advanced",
        Author = "microdee",
        AutoEvaluate = true
        )]
    public class RawMemoryMappedFile : IPluginEvaluate
    {
        [Input("Name")]
        public ISpread<string> FName;

        [Input("Size")]
        public ISpread<int> FSize;

        [Input("Offset")]
        public ISpread<int> FOffset;

        [Input("Open", IsBang = true)]
        public ISpread<bool> FOpen;

        [Input("Read", IsBang = true)]
        public ISpread<bool> FRead;

        [Output("Output")]
        public ISpread<Stream> FOutput;

        [Output("Memory Mapped File", Visibility = PinVisibility.Hidden)]
        public ISpread<MemoryMappedFile> FMmf;

        private readonly Dictionary<string, MemoryMappedFile> _mmfs = new Dictionary<string, MemoryMappedFile>();

        public void Evaluate(int SpreadMax)
        {
            FOutput.Resize(SpreadMax, () => new MemoryComStream(), str => str.Dispose());
            FMmf.SliceCount = SpreadMax;
            for (int i = 0; i < SpreadMax; i++)
            {
                if(string.IsNullOrWhiteSpace(FName[i])) continue;
                if (FOpen[i] && !_mmfs.ContainsKey(FName[i]))
                {
                    try
                    {
                        var mmf = MemoryMappedFile.OpenExisting(FName[i]);
                        _mmfs.Add(FName[i], mmf);
                    }
                    catch
                    { }
                }

                if ((FOpen[i] || FRead[i]) && _mmfs.ContainsKey(FName[i]))
                {
                    var mmf = _mmfs[FName[i]];
                    FMmf[i] = mmf;
                    if (FOutput[i] == null) FOutput[i] = new MemoryComStream();
                    if (FSize[i] == 0)
                    {
                        using (var accessor = mmf.CreateViewStream())
                        {
                            FOutput[i].SetLength(accessor.Length);
                            accessor.Position = 0;
                            FOutput[i].Position = 0;
                            accessor.CopyTo(FOutput[i]);
                            FOutput[i].Position = 0;
                        }
                    }
                    else
                    {
                        using (var accessor = mmf.CreateViewStream(FOffset[i], FSize[i]))
                        {
                            FOutput[i].SetLength(accessor.Length);
                            accessor.Position = 0;
                            FOutput[i].Position = 0;
                            accessor.CopyTo(FOutput[i]);
                            FOutput[i].Position = 0;
                        }
                    }
                }
            }

            FOutput.Stream.IsChanged = false;
            if (FRead.Any() || FOpen.Any())
            {
                FOutput.Flush(true);
            }
        }
    }
}
