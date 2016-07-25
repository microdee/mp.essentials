#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Evaluate", Category = "Node", Help = "Simple AutoEvaluate", AutoEvaluate = true)]
	#endregion PluginInfo
	public class NodeEvaluateNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		[Import]
		public IPluginHost PluginHost;
		
        public INodeIn Pin;
		
		public void OnImportsSatisfied()
		{
            PluginHost.CreateNodeInput("Input", TSliceMode.Dynamic, TPinVisibility.True, out Pin);
            Pin.SetSubType2(null, new Guid[] { }, "Variant");
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			
		}
	}
}
