using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using md.stdl.Coding;
using md.stdl.Windows;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.windows
{
    public class FileDeletionTask
    {
        public string Folder;
        public bool KeepEmptyFolder;
        public int Id;
        public Exception Exception;
        public bool Complete;
        public bool DeleteMe;
    }

    [PluginInfo(
        Name = "DeleteFolder",
        Category = "File",
        Author = "microdee",
        Help = "Deletes a folder recursively and asynchronously"
    )]
    public class FileDeleteFolderNode : IPluginEvaluate
    {
        [Input("Folder", StringType = StringType.Directory)]
        public ISpread<string> FolderPathIn;

        [Input("Keep Folder")]
        public ISpread<bool> KeepIn;

        [Input("Delete", IsBang = true)]
        public ISpread<bool> DoDeleteIn;

        [Output("Folder Out")]
        public ISpread<string> FolderOut;

        [Output("Complete", IsBang = true)]
        public ISpread<bool> CompleteOut;

        [Output("Exception")]
        public ISpread<Exception> ExceptionOut;

        private ConcurrentDictionary<string, FileDeletionTask> _tasks =
            new ConcurrentDictionary<string, FileDeletionTask>();

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < FolderPathIn.SliceCount; i++)
            {
                if (DoDeleteIn[i] && !_tasks.ContainsKey(FolderPathIn[i]))
                {
                    var ii = i;
                    FileDeletionTask obj = new FileDeletionTask
                    {
                        Folder = FolderPathIn[i],
                        KeepEmptyFolder = KeepIn[i],
                        Id = ii
                    };
                    if(_tasks.TryAdd(FolderPathIn[i], obj));
                    {
                        var task = Task.Run(() =>
                        {
                            try
                            {
                                FileSystem.DeleteDirectory(obj.Folder, true);
                                if (obj.KeepEmptyFolder)
                                {
                                    while (Directory.Exists(obj.Folder))
                                    {
                                        Thread.Sleep(100);
                                    }
                                    Directory.CreateDirectory(obj.Folder);
                                }
                            }
                            catch (Exception e)
                            {
                                obj.Exception = e;
                            }
                            obj.Complete = true;
                        });
                    }
                }
            }

            FolderOut.SliceCount = 0;
            CompleteOut.SliceCount = 0;
            ExceptionOut.SliceCount = 0;

            _tasks.ForeachConcurrent((f, t) =>
            {
                if (t.DeleteMe) _tasks.TryRemove(f, out _);
                else
                {
                    FolderOut.Add(f);
                    ExceptionOut.Add(t.Exception);
                    CompleteOut.Add(t.Complete);

                    if (t.Complete) t.DeleteMe = true;
                }
            });
        }
    }
}
