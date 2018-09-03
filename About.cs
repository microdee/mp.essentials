using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interfaces;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials
{
    [Startable]
    public class VersionWriter : IStartable
    {
        public static string VvvvDir => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        public static string VersionPath { get; private set; }
        public static string PackDir => Path.GetDirectoryName(typeof(VersionWriter).Assembly.Location);

        private void WriteVersion()
        {
            var ver = typeof(VersionWriter).Assembly.GetName().Version.ToString();
            if (VvvvDir != null)
            {
                VersionPath = Path.Combine(VvvvDir, "packs", "mp.essentials", "version.info");
                File.WriteAllText(VersionPath, ver);
            }
            else
            {
                VersionPath = "null";
            }
        }

        public void Start()
        {
            WriteVersion();
        }

        public void Shutdown() { WriteVersion(); }
    }

    [PluginInfo(
        Name = "About",
        Category = "Mp.Essentials",
        Author = "microdee"
    )]
    public class AboutNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Output("mp.essentials Version")] public ISpread<string> FVer;
        [Output("md.stdl Version")] public ISpread<string> FMdStdlVer;
        [Output("mp.pddn Version")] public ISpread<string> FMpPddnVer;

        [Output("Version File Path")] public ISpread<string> FVerPath;
        [Output("VVVV Dir")] public ISpread<string> FVDir;

        public void Evaluate(int SpreadMax) { }
        public void OnImportsSatisfied()
        {
            FVer[0] = typeof(AboutNode).Assembly.GetName().Version.ToString();
            FMdStdlVer[0] = typeof(IMainlooping).Assembly.GetName().Version.ToString();
            FMpPddnVer[0] = typeof(SpreadWrapper).Assembly.GetName().Version.ToString();

            FVDir[0] = VersionWriter.VvvvDir;
            FVerPath[0] = VersionWriter.VersionPath;
        }
    }
}
