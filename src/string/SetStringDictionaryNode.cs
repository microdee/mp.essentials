#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Strings
{
	[PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "Set",
        Author = "microdee"
        )]
	public class SetStringDictionaryNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Keys")]
		public IDiffSpread<string> FKeys;
		[Input("Default")]
		public ISpread<string> FDef;
		[Input("Modify")]
		public ISpread<string> FMod;
		[Input("Value")]
		public ISpread<string> FVal;
		[Input("Set", IsBang=true)]
		public ISpread<bool> FSet;

		[Output("Keys Out")]
		public ISpread<string> FKeysOut;
		[Output("Values")]
		public ISpread<string> FOut;
		
		private Dictionary<string, string> dict = new Dictionary<string, string>();

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FKeys.IsChanged)
			{
				dict.Clear();
				if(FDef.SliceCount != 0)
				{
					for(int i=0; i<FKeys.SliceCount; i++)
					{
						if(!dict.ContainsKey(FKeys[i]))
							dict.Add(FKeys[i], FDef[i]);
					}
				}
			}
			if(FSet[0])
			{
				if(FVal.SliceCount != 0)
				{
					for(int i=0; i<FMod.SliceCount; i++)
					{
						if(dict.ContainsKey(FMod[i]))
							dict[FMod[i]] = FVal[i];
					}
				}
			}
			if(FKeys.IsChanged || FSet[0])
			{
				FOut.SliceCount = dict.Count;
				FKeysOut.SliceCount = dict.Count;
				int i=0;
				foreach(var kvp in dict)
				{
					FOut[i] = kvp.Value;
					FKeysOut[i] = kvp.Key;
					i++;
				}
			}
			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
