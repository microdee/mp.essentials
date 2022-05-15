using System.IO;
using System.Linq;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.windows
{
    [PluginInfo(
        Name = "DirectoryExists",
        Category = "File",
        Author = "microdee"
    )]
    public class DirectoryExists : IPluginEvaluate
    {
        [Input("Directory", StringType = StringType.Directory)]
        public IDiffSpread<string> DirectoryIn;
        
        [Input("Update", IsBang = true)]
        public ISpread<bool> UpdateIn;
        
        [Output("Exists")]
        public ISpread<bool> ExistsOut;

        public void Evaluate(int SpreadMax)
        {
            if (DirectoryIn.IsChanged || UpdateIn.TryGetSlice(0))
            {
                ExistsOut.AssignFrom(DirectoryIn.Select(Directory.Exists));
            }
        }
    }
}