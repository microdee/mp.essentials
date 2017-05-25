using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace VVVV.Nodes.PDDN
{
    public static class NodeExtensions
    {
        public static void SetSliceCountForAllOutput(this IPluginEvaluate node, int sc, string[] ignore = null)
        {
            foreach (var field in node.GetType().GetFields())
            {
                if(ignore != null)
                    if (ignore.Contains(field.Name)) continue;
                if (field.GetCustomAttributes(typeof(OutputAttribute), false).Length == 0) continue;
                var spread = (NGISpread)field.GetValue(node);
                spread.SliceCount = sc;
            }
        }
    }
}
