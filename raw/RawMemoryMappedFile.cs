using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mp.pddn;
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
        Name = "CreateMutex",
        Category = "Raw",
        Version = "SharedMemory.Advanced",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class RawCreateMutex : IPluginEvaluate
    {
        [Input("Name")]
        public ISpread<string> NameIn;

        [Input("Create", IsBang = true)]
        public IDiffSpread<bool> CreateIn;

        [Output("Mutex")]
        public ISpread<Mutex> MutexOut;

        private readonly Dictionary<string, Mutex> _mutexes = new Dictionary<string, Mutex>();

        public void Evaluate(int SpreadMax)
        {
            if (!CreateIn.IsChanged) return;

            MutexOut.SliceCount = NameIn.SliceCount;

            for (int i = 0; i < NameIn.SliceCount; i++)
            {
                if (!CreateIn[i]) continue;
                if (string.IsNullOrWhiteSpace(NameIn[i])) continue;

                Mutex mutex = null;
                bool createNew = false;
                if (_mutexes.ContainsKey(NameIn[i]))
                {
                    mutex = _mutexes[NameIn[i]];
                }
                else
                {
                    if(!Mutex.TryOpenExisting(NameIn[i], out mutex))
                        mutex = new Mutex(true, NameIn[i]);
                    _mutexes.Add(NameIn[i], mutex);
                }

                MutexOut[i] = mutex;
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

        [Input("Mutex", Visibility = PinVisibility.Hidden)]
        public ISpread<Mutex> MutexIn;

        [Output("Output")]
        public ISpread<Stream> FOutput;

        public void Evaluate(int SpreadMax)
        {
            FOutput.Resize(FMmf.SliceCount, () => new MemoryComStream(), str => str.Dispose());
            for (int i = 0; i < FMmf.SliceCount; i++)
            {
                if (FMmf[i] != null && FRead[i])
                {
                    var mutex = MutexIn.TryGetSlice(i);

                    var mmf = FMmf[i];
                    FMmf[i] = mmf;
                    if (FOutput[i] == null) FOutput[i] = new MemoryComStream();
                    if (FSize[i] == 0)
                    {
                        mutex?.WaitOne();
                        using (var accessor = mmf.CreateViewStream())
                        {
                            FOutput[i].SetLength(accessor.Length);
                            accessor.Position = 0;
                            FOutput[i].Position = 0;
                            accessor.CopyTo(FOutput[i]);
                            FOutput[i].Position = 0;
                        }
                        mutex?.ReleaseMutex();
                    }
                    else
                    {
                        mutex?.WaitOne();
                        using (var accessor = mmf.CreateViewStream(FOffset[i], FSize[i]))
                        {
                            FOutput[i].SetLength(accessor.Length);
                            accessor.Position = 0;
                            FOutput[i].Position = 0;
                            accessor.CopyTo(FOutput[i]);
                            FOutput[i].Position = 0;
                        }
                        mutex?.ReleaseMutex();
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

        [Input("Mutex", Visibility = PinVisibility.Hidden)]
        public ISpread<Mutex> MutexIn;

        private Spread<byte[]> _buffers = new Spread<byte[]>();

        private void HandleStream(int i, MemoryMappedViewStream accessor)
        {
            if(_buffers[i] == null) _buffers[i] = new byte[accessor.Length];
            if(_buffers[i].Length < accessor.Length) _buffers[i] = new byte[accessor.Length];
            FInput[i].Position = 0;
            FInput[i].Read(_buffers[i], 0, _buffers[i].Length);
            FInput[i].Position = 0;
            accessor.Position = 0;
            accessor.Write(_buffers[i], 0, _buffers[i].Length);
            accessor.Position = 0;
        }

        public void Evaluate(int SpreadMax)
        {
            _buffers.SliceCount = FMmf.SliceCount;
            for (int i = 0; i < FMmf.SliceCount; i++)
            {
                var mmf = FMmf[i];
                if(mmf == null) continue;
                if(!FWrite[i]) continue;

                var mutex = MutexIn.TryGetSlice(i);

                if (FSize[i] == 0)
                {
                    mutex?.WaitOne();
                    using (var accessor = mmf.CreateViewStream())
                    {
                        HandleStream(i, accessor);
                    }
                    mutex?.ReleaseMutex();
                }
                else
                {
                    mutex?.WaitOne();
                    using (var accessor = mmf.CreateViewStream(FOffset[i], FSize[i]))
                    {
                        HandleStream(i, accessor);
                    }
                    mutex?.ReleaseMutex();
                }
            }
        }
    }
}
