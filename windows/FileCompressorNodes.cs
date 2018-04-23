using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using SharpCompress.Archives;
using SharpCompress.Readers;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.windows
{
    public class ExtractionProgress
    {
        public Task ExtractionTask;
        public string Destination;
        public string SourceArchive;
        public IArchive Archive;
        public IReader ArchiveReader;
        public long TotalBytes;
        public long CurrentBytes;
        public double ByteProgress;
        public long FileTotalBytes;
        public long FileCurrentBytes;
        public double FileByteProgress;
        public string CurrentFile;
        public bool Completed;
    }

    [PluginInfo(
        Name = "DecompressionProgressInfo",
        Category = "File",
        Author = "Microdee"
    )]
    public class DecompressionProgressInfoNode : ObjectSplitNode<ExtractionProgress> { }

    [PluginInfo(
        Name = "Decompress",
        Category = "File",
        Author = "Microdee"
    )]
    public class FileDecompressNode : IPluginEvaluate
    {
        [Input("Archive File", StringType = StringType.Filename)]
        public ISpread<string> FSrc;
        [Input("Destination Folder", StringType = StringType.Directory)]
        public ISpread<string> FDstDir;
        [Input("Decompress", IsBang = true)]
        public ISpread<bool> FDecompress;

        [Output("Archive")]
        public ISpread<ExtractionProgress> FExtractData;

        Dictionary<string, ExtractionProgress> _archives = new Dictionary<string, ExtractionProgress>();

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < SpreadMax; i++)
            {
                if (FDecompress[i])
                {
                    var dstdir = FDstDir[i];
                    var src = FSrc[i];
                    if (_archives.ContainsKey(dstdir + src)) continue;

                    var extractprog = new ExtractionProgress
                    {
                        Archive = ArchiveFactory.Open(src),
                        Destination = dstdir,
                        SourceArchive = src
                    };
                    extractprog.ExtractionTask = Task.Factory.StartNew(() =>
                    {
                        var reader = extractprog.ArchiveReader = extractprog.Archive.ExtractAllEntries();
                        extractprog.TotalBytes = extractprog.Archive.TotalSize;
                        extractprog.Archive.CompressedBytesRead += (sender, args) =>
                        {
                            extractprog.CurrentBytes = args.CompressedBytesRead;
                            extractprog.ByteProgress = (double)args.CompressedBytesRead / extractprog.TotalBytes;
                        };
                        reader.EntryExtractionProgress += (sender, args) =>
                        {
                            extractprog.CurrentFile = args.Item.Key;
                            extractprog.FileTotalBytes = args.Item.Size;
                            extractprog.FileCurrentBytes = args.ReaderProgress.BytesTransferred;
                            extractprog.FileByteProgress = args.ReaderProgress.PercentageReadExact;
                        };
                        reader.WriteAllToDirectory(extractprog.Destination, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                            PreserveAttributes = false,
                            PreserveFileTime = false
                        });
                    });
                    _archives.Add(dstdir + src, extractprog);
                }
            }

            FExtractData.SliceCount = _archives.Count;
            int ii = 0;
            foreach (var archive in _archives.Values)
            {
                archive.Completed = archive.ExtractionTask.IsCompleted;
                FExtractData[ii] = archive;
                ii++;
            }

            foreach (var archive in _archives.Values.Where(a => a.Completed).ToArray())
            {
                archive.Archive.Dispose();
                archive.ArchiveReader.Dispose();
                _archives.Remove(archive.Destination + archive.SourceArchive);
            }
        }
    }
}
