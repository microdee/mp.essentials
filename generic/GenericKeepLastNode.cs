#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using mp.pddn;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2.NonGeneric;

#endregion usings

namespace mp.essentials.Nodes.Generic
{
	public abstract class KeepLastNode<T> : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<ISpread<T>> FInput;
		[Input("Default")]
		public ISpread<T> FDef;
		[Input("Reset", IsBang = true)]
		public ISpread<bool> FReset;

		[Output("Output")]
		public ISpread<ISpread<T>> FOutput;
		[Output("Has Value")]
		public ISpread<bool> FHasValue;
		[Output("Is NIL")]
		public ISpread<bool> FIsNIL;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			FHasValue.SliceCount = FInput.SliceCount;
			for(int i=0; i<FInput.SliceCount; i++)
			{
				if(FReset[i])
				{
					FHasValue[i] = false;
				}
				if(!FHasValue[i])
				{
					FOutput[i].SliceCount = 1;
					FOutput[i][0] = FDef[i];
				}
				if(FInput[i].SliceCount != 0)
				{
					FHasValue[i] = true;
					FIsNIL[i] = false;
					FOutput[i].SliceCount = FInput[i].SliceCount;
					for(int j=0; j<FInput[i].SliceCount; j++)
					{
						FOutput[i][j] = FInput[i][j];
					}
				}
				else
				{
					FIsNIL[i] = true;
				}
			}
		}
	}

    [PluginInfo(
        Name = "KeepLast",
        Category = "Generic",
        Tags = "",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class GenericKeepLastNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
        private bool _typeChanged;
        private bool _pgready;
        private DiffSpreadPin _input;
        private DiffSpreadPin _default;
        private SpreadPin _output;

        [Input("Reset", IsBang = true)]
        public ISpread<bool> FReset;
        
        [Output("Has Value")]
        public ISpread<bool> FHasValue;
        [Output("Is NIL")]
        public ISpread<bool> FIsNIL;


        public void OnImportsSatisfied()
        {
            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input");
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;
                if(_pgready) return;
                _pgready = true;

                _input = _pg.AddInputBinSized(new InputAttribute("Input") { Order = 10 });
                _default = _pg.AddInput(new InputAttribute("Default") {Order = 10});
                _output = _pg.AddOutputBinSized(new OutputAttribute("Output"));
            };
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (_typeChanged) _typeChanged = false;
            if (!_pgready) return;

            _output.Spread.SliceCount = _input.Spread.SliceCount;
            FHasValue.SliceCount = _input.Spread.SliceCount;
            for (int i = 0; i < _input.Spread.SliceCount; i++)
            {
                var outspread = (ISpread) _output[i];
                var inspread = (ISpread) _input[i];
                if (FReset[i])
                {
                    FHasValue[i] = false;
                }
                if (!FHasValue[i])
                {
                    outspread.SliceCount = 1;
                    outspread[0] = _default[i];
                }
                if (inspread.SliceCount != 0)
                {
                    FHasValue[i] = true;
                    FIsNIL[i] = false;
                    outspread.SliceCount = inspread.SliceCount;
                    for (int j = 0; j < inspread.SliceCount; j++)
                    {
                        outspread[j] = inspread[j];
                    }
                }
                else
                {
                    FIsNIL[i] = true;
                }
            }
        }
    }

    // Miss a type? Can you see the pattern here? ;)

    [PluginInfo(
        Name = "KeepLast",
        Category = "Value",
        Tags = "",
        Author = "microdee"
        )]
	public class ValueKeepLastNode : KeepLastNode<double> { }
	
	[PluginInfo(
        Name = "KeepLast",
        Category = "String",
        Tags = "",
        Author = "microdee"
        )]
    public class StringKeepLastNode : KeepLastNode<string> { }
	
	[PluginInfo(
        Name = "KeepLast",
        Category = "Transform",
        Tags = "",
        Author = "microdee"
        )]
    public class TransformKeepLastNode : KeepLastNode<Matrix4x4> { }
}
