#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "CustomEnum", 
	            Category = "Enumerations",
				AutoEvaluate = true,
	            Tags = "")]
	#endregion PluginInfo
	public class EnumerationsCustomEnum : IPluginEvaluate
	{
		#region fields & pins
		[Input("UpdateEnum", IsBang = true)]
        public ISpread<bool> FChangeEnum;
		
		[Input("EnumName", DefaultString = "MyEnum")]
        public ISpread<string> FEnumName;

		[Input("Enum Entries", DefaultString = "MyEnumEntry")]
        public ISpread<ISpread<string>> FEnumStrings;

		[Import()]
        public ILogger Flogger;
		#endregion fields & pins
		
		protected bool init = true;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			for(int i=0; i<FEnumName.SliceCount; i++)
			{
				bool valid = (FChangeEnum[i]) && (FEnumStrings[i].SliceCount > 0);
				valid = valid || init;
				if (valid)
				{
					EnumManager.UpdateEnum(FEnumName[i], 
						FEnumStrings[i][0], FEnumStrings[i].ToArray());	
				}
			}
			if(init) init = false;
		}
	}
}
