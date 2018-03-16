using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials
{
    [PluginInfo(
        Name = "About",
        Category = "mp.essentials",
        Author = "microdee"
    )]
    public class AboutNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Output("Version")] public ISpread<string> FVer;

        public void Evaluate(int SpreadMax) { }
        public void OnImportsSatisfied()
        {
            FVer[0] = typeof(AboutNode).Assembly.GetName().Version.ToString();
        }
    }
}
