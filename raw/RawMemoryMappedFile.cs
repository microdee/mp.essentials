using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.IO;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;

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

        [Output("Memory Mapped File")]
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

    [PluginInfo(
        Name = "Create",
        Category = "Raw",
        Version = "SharedMemory.Advanced",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class RawCreateMemoryMappedFile : IPluginEvaluate
    {
        [Input("Name")]
        public ISpread<string> FName;

        [Input("Size")]
        public ISpread<int> FSize;

        [Input("Create", IsBang = true)]
        public IDiffSpread<bool> FCreate;

        [Output("Memory Mapped File")]
        public ISpread<MemoryMappedFile> FMmf;

        private readonly Dictionary<string, MemoryMappedFile> _mmfs = new Dictionary<string, MemoryMappedFile>();

        public void Evaluate(int SpreadMax)
        {
            if(!FCreate.IsChanged) return;

            FMmf.SliceCount = FName.SliceCount;

            for (int i = 0; i < FName.SliceCount; i++)
            {
                if (!FCreate[i]) continue;
                if(string.IsNullOrWhiteSpace(FName[i])) continue;

                MemoryMappedFile mmf = null;
                bool createNew = false;
                if (_mmfs.ContainsKey(FName[i]))
                {
                    mmf = _mmfs[FName[i]];
                }
                else
                {
                    mmf = MemoryMappedFile.CreateOrOpen(FName[i], FSize[i], MemoryMappedFileAccess.ReadWrite);
                    _mmfs.Add(FName[i], mmf);
                }

                FMmf[i] = mmf;
            }
        }
    }

    [PluginInfo(
        Name = "ReadExisting",
        Category = "Raw",
        Version = "SharedMemory.Advanced",
        Author = "microdee",
        AutoEvaluate = true
        )]
    public class RawMemoryMappedFileReader : IPluginEvaluate
    {
        [Input("Memory Mapped File")]
        public ISpread<MemoryMappedFile> FMmf;

        [Input("Size")]
        public ISpread<int> FSize;

        [Input("Offset")]
        public ISpread<int> FOffset;

        [Input("Read", IsBang = true)]
        public ISpread<bool> FRead;

        [Output("Output")]
        public ISpread<Stream> FOutput;

        public void Evaluate(int SpreadMax)
        {
            FOutput.Resize(FMmf.SliceCount, () => new MemoryComStream(), str => str.Dispose());
            for (int i = 0; i < FMmf.SliceCount; i++)
            {
                if (FMmf[i] != null && FRead[i])
                {
                    var mmf = FMmf[i];
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
            if (FRead.Any())
            {
                FOutput.Flush(true);
            }
        }
    }

    [PluginInfo(
        Name = "WriteExisting",
        Category = "Raw",
        Version = "SharedMemory.Advanced",
        Author = "microdee",
        AutoEvaluate = true
        )]
    public class RawMemoryMappedFileWriter : IPluginEvaluate
    {
        [Input("Memory Mapped File")]
        public ISpread<MemoryMappedFile> FMmf;

        [Input("Input")]
        public ISpread<Stream> FInput;

        [Input("Size")]
        public ISpread<int> FSize;

        [Input("Offset")]
        public ISpread<int> FOffset;

        [Input("Write", IsBang = true)]
        public ISpread<bool> FWrite;

        private void HandleStream(int i, MemoryMappedViewStream accessor)
        {
            var bytes = new byte[accessor.Length];
            FInput[i].Position = 0;
            FInput[i].Read(bytes, 0, bytes.Length);
            FInput[i].Position = 0;
            accessor.Position = 0;
            accessor.Write(bytes, 0, bytes.Length);
            accessor.Position = 0;
        }

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < FMmf.SliceCount; i++)
            {
                var mmf = FMmf[i];
                if(mmf == null) continue;
                if(!FWrite[i]) continue;

                if (FSize[i] == 0)
                {
                    using (var accessor = mmf.CreateViewStream())
                    {
                        HandleStream(i, accessor);
                    }
                }
                else
                {
                    using (var accessor = mmf.CreateViewStream(FOffset[i], FSize[i]))
                    {
                        HandleStream(i, accessor);
                    }
                }
            }
        }
    }
}
