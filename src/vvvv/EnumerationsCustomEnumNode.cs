#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using VVVV.Nodes.PDDN;

#endregion usings

namespace mp.essentials.Nodes.vvvv
{
	[PluginInfo(Name = "CustomEnum", 
	            Category = "Enumerations",
				AutoEvaluate = true,
                Author = "microdee"
        )]
	public class EnumerationsCustomEnum : IPartImportsSatisfiedNotification, IPluginEvaluate
	{

        #region fields & pins

	    [Input("Enum Entries", DefaultString = "MyEnumEntry")]
	    public IDiffSpread<string> FEnumStrings;
        
	    [Output("Actual Enum Name")]
	    public ISpread<string> FOutEnumName;
	    [Output("Selected Index")]
	    public ISpread<int> FSelIndex;
	    [Output("Selected Text")]
	    public ISpread<string> FSelText;

	    protected Pin<EnumEntry> EnumSample;

        [Import]
        public ILogger Flogger;

	    [Import]
	    public IPluginHost2 FHost;

        [Import]
        public IHDEHost FHdeHost;

	    [Import]
	    public IIOFactory FIoFactory;
        #endregion fields & pins

        protected bool frameinit = true;
        protected string NodePath = "";

	    public void OnImportsSatisfied()
	    {
	        NodePath = "CustomEnum_" + FHost.GetNodePath(false);

	        EnumSample = FIoFactory.CreatePin<EnumEntry>(new InputAttribute("Enum Sample")
	        {
	            EnumName = NodePath
	        });
	        UpdateEnumAndPins();

	    }

	    void UpdateEnumAndPins()
	    {
	        FOutEnumName.SliceCount = 1;
	        FOutEnumName[0] = NodePath;

	        var def = FEnumStrings.SliceCount == 0 ? "(nil)" : FEnumStrings[0];
	        FHost.UpdateEnum(NodePath, def, FEnumStrings.ToArray());
        }
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FEnumStrings.SliceCount == 0 || EnumSample == null) return;
            if (frameinit)
			{
			    frameinit = false;
                UpdateEnumAndPins();
			}

		    if (FEnumStrings.IsChanged)
		    {
                UpdateEnumAndPins();
		    }

		    if (EnumSample.IsChanged && EnumSample.SliceCount != 0)
		    {
                FSelIndex[0] = EnumSample[0].Index;
		        FSelText[0] = EnumSample[0].Name;
            }
		}
	}
}
