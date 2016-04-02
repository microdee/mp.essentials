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
	[PluginInfo(Name = "NodePath", Category = "VVVV", Tags = "")]
	#endregion PluginInfo
	public class VVVVNodePathNode : IPluginEvaluate
	{
		[Import]
		IPluginHost2 PluginHost;
		
		[Output("Node Path")]
		public ISpread<string> FNode;
		[Output("Host Path")]
		public ISpread<string> FHost;

		[Import()]
		public ILogger FLogger;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			string hostpath, nodepath;
			this.PluginHost.GetHostPath(out hostpath);
			this.PluginHost.GetNodePath(false, out nodepath);
			FHost[0] = hostpath;
			FNode[0] = nodepath;

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
