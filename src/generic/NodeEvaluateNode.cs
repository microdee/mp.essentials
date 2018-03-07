#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using mp.pddn;
using VVVV.Utils.Reflection;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

#endregion usings

namespace mp.essentials.Nodes.Generic
{
	#region PluginInfo
	[PluginInfo(
        Name = "Evaluate",
        Category = "Node",
        Help = "Simple AutoEvaluate",
        AutoEvaluate = true)]
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

    public abstract class PerformerNode<T> : IPluginEvaluate
    {
        [Input("Input", AutoValidate = false)] public ISpread<T> FInput;
        [Input("Default")] public ISpread<T> FDefault;
        [Input("Execute", IsBang = true)] public ISpread<bool> FEval;
        [Output("Output")] public ISpread<T> FOutput;

        private bool Initial = true;

        public void Evaluate(int SpreadMax)
        {
            FOutput.Stream.IsChanged = false;
            if (Initial)
            {
                FOutput.AssignFrom(FDefault);
                Initial = false;
                FOutput.Flush();
                FOutput.Stream.IsChanged = true;
            }
            if (FEval[0])
            {
                FInput.Sync();
                FOutput.AssignFrom(FInput);
                FOutput.Flush();
                FOutput.Stream.IsChanged = true;
            }
        }
    }

    [PluginInfo(
        Name = "Performer",
        Category = "Transform",
        Help = "Manually evaluate upstream nodes",
        AutoEvaluate = true)]
    public class TransformPerformerNode : PerformerNode<Matrix4x4> { }
    [PluginInfo(
        Name = "Performer",
        Category = "Value",
        Help = "Manually evaluate upstream nodes",
        AutoEvaluate = true)]
    public class ValuePerformerNode : PerformerNode<double> { }
    [PluginInfo(
        Name = "Performer",
        Category = "String",
        Help = "Manually evaluate upstream nodes",
        AutoEvaluate = true)]
    public class StringPerformerNode : PerformerNode<string> { }

    [PluginInfo(
        Name = "Performer",
        Category = "Node",
        Help = "Manually evaluate upstream nodes of any CLR type",
        AutoEvaluate = true)]
    public class NodePerformerNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        [Config("Type", DefaultString = "")] public IDiffSpread<string> FType;
        public GenericInput FRefType;
        [Input("Learnt Type Inheritence Level", Order = 2, Visibility = PinVisibility.Hidden, DefaultValue = 0)]
        public ISpread<int> FTypeInheritence;
        [Input("Learn Type", Order = 3, IsBang = true)] public ISpread<bool> FLearnType;

        [Input("Execute", IsBang = true, Order = 4)]
        public ISpread<bool> FEval;

        protected Type CType;
        protected int InitDescSet = 0;
        protected PinDictionary Pd;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FType;
            FRefType = new GenericInput(FPluginHost, new InputAttribute("Reference Type")
            {
                Order = 1
            });
            Pd = new PinDictionary(FIOFactory);
            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, "");
                break;
            }
        }
        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }
        protected override void OnConfigPinChanged()
        {
            FType.Stream.IsChanged = false;
            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, "");
                break;
            }
            if (IsConfigDefault()) return;
            Pd.RemoveAllInput();
            Pd.RemoveAllOutput();
            CType = Type.GetType(FType[0]);
            if (CType == null) return;

            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, CType.GetCSharpName());
                break;
            }
            Pd.AddInput(CType, new InputAttribute("Input")
            {
                Order = 0,
                AutoValidate = false
            });
            Pd.AddOutput(CType, new OutputAttribute("Output"));
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FLearnType[0])
            {
                try
                {
                    var types = FRefType[0].GetType().GetTypes().ToArray();
                    FType[0] = types[Math.Min(FTypeInheritence[0], types.Length - 1)].AssemblyQualifiedName;
                    FType.Stream.IsChanged = true;
                }
                catch (Exception e)
                { }
            }
            if (IsConfigDefault()) return;
            if (InitDescSet < 5)
            {
                foreach (var pin in FPluginHost.GetPins())
                {
                    if (pin.Name != "Descriptive Name") continue;
                    pin.SetSlice(0, CType?.GetCSharpName());
                    InitDescSet++;
                    break;
                }
            }
            if (FEval[0])
            {
                Pd.InputPins["Input"].Spread.Sync();
                Pd.OutputPins["Output"].Spread.SliceCount = Pd.InputPins["Input"].Spread.SliceCount;
                for (int i = 0; i < Pd.InputPins["Input"].Spread.SliceCount; i++)
                {
                    Pd.OutputPins["Output"].Spread[i] = Pd.InputPins["Input"].Spread[i];
                }
                Pd.OutputPins["Output"].Spread.Flush();
            }
        }
    }
}
