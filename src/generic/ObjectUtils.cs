using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.PDDN;
using VVVV.Utils.Reflection;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace VVVV.Nodes.VObjects
{

    [PluginInfo(
         Name = "GetType",
         Category = "Object",
         Author = "microdee"
     )]
    public class ObjectGetTypeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] public IPluginHost2 FPluginHost;

        protected GenericInput FInput;

        [Output("Object Type")] public ISpread<string> FType;

        public void OnImportsSatisfied()
        {
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input"));
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected)
            {
                FType.SliceCount = FInput.Pin.SliceCount;
                for (int i = 0; i < FInput.Pin.SliceCount; i++)
                {
                    FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                }
            }
            else
            {
                FType.SliceCount = 0;
            }
        }
    }
    [PluginInfo(
        Name = "GetType",
        Category = "Object",
        Version = "PluginInterfaces.V2",
        Author = "microdee"
     )]
    public class V2ObjectGetTypeNode : IPluginEvaluate
    {
        [Import]
        public IPluginHost2 FPluginHost;

        [Input("Input")]
        public Pin<object> FInput;

        [Output("Object Type")]
        public ISpread<string> FType;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsConnected)
            {
                FType.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                }
            }
            else
            {
                FType.SliceCount = 0;
            }
        }
    }

    [PluginInfo(
         Name = "FilterType",
         Category = "Object",
         Help = "Filter objects by type",
         Author = "microdee",
         AutoEvaluate = true)]
    public class ObjectFilterTypeNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] public IPluginHost2 FPluginHost;
        [Import] public IIOFactory FIOFactory;

        protected GenericInput FInput;
        protected GenericInput FTypeRef;

        [Config("Type", DefaultString = "")] public IDiffSpread<string> FType;

        public PinDictionary pd;

        protected override void PreInitialize()
        {
            pd = new PinDictionary(FIOFactory);
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input")) {Pin = {Order = 0}};
            FTypeRef = new GenericInput(FPluginHost, new InputAttribute("Type Reference Object")) {Pin = {Order = 1}};
            ConfigPinCopy = FType;
        }

        private Type CType;

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void Initialize()
        {
            if (FType[0] != "")
            {
                CType = Type.GetType(FType[0], true);

                if (FTypeRef.Pin.SliceCount != 0)
                {
                    if (FTypeRef[0] != null)
                    {
                        Type T = FTypeRef[0].GetType();
                        if (T != CType)
                        {
                            pd.RemoveAllOutput();
                            pd.AddOutputBinSized(T, new OutputAttribute("Output"));
                            CType = T;
                            FType[0] = T.AssemblyQualifiedName;
                        }
                    }
                }

                //RemoveAllOutput();
                pd.AddOutputBinSized(CType, new OutputAttribute("Output"));
            }
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected && FTypeRef.Connected)
            {
                if (FTypeRef.Pin.SliceCount != 0)
                {
                    Type T = FTypeRef[0].GetType();
                    bool valid = false;
                    if (CType == null)
                        valid = true;
                    else
                        valid = T != CType;
                    if (valid)
                    {
                        pd.RemoveAllOutput();
                        pd.AddOutputBinSized(T, new OutputAttribute("Output"));
                        CType = T;
                        FType[0] = T.AssemblyQualifiedName;
                    }
                }
                if (pd.OutputPins.ContainsKey("Output"))
                {
                    pd.OutputPins["Output"].Spread.SliceCount = FInput.Pin.SliceCount;
                    for (int i = 0; i < FInput.Pin.SliceCount; i++)
                    {
                        var cspread = (NGISpread) pd.OutputPins["Output"].Spread[i];
                        if (CType == FInput[i].GetType())
                        {
                            cspread.SliceCount = 1;
                            cspread[0] = FInput[i];
                        }
                        else
                        {
                            cspread.SliceCount = 0;
                        }
                    }
                }
            }
            else
            {
                if (pd.OutputPins.ContainsKey("Output"))
                {
                    pd.OutputPins["Output"].Spread.SliceCount = 0;
                }
            }
        }
    }

    [PluginInfo(
         Name = "FilterType",
         Category = "Object",
         Version = "TypeName",
         Help = "Filter objects by type name",
         Author = "microdee",
         AutoEvaluate = true)]
    public class TypeNameObjectFilterTypeNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] public IPluginHost2 FPluginHost;
        [Import] public IIOFactory FIOFactory;

        protected GenericInput FInput;

        [Config("Type", DefaultString = "")] public IDiffSpread<string> FType;

        public PinDictionary pd;

        protected override void PreInitialize()
        {
            pd = new PinDictionary(FIOFactory);
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input")) {Pin = {Order = 0}};
            ConfigPinCopy = FType;
        }

        private Type CType;

        protected override void OnConfigPinChanged()
        {
            CType = Type.GetType(FType[0], true);
            pd.RemoveAllOutput();
            pd.AddOutputBinSized(CType, new OutputAttribute("Output"));
        }

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void Initialize()
        {
            if (FType[0] != "")
            {
                CType = Type.GetType(FType[0], true);
                //RemoveAllOutput();
                pd.AddOutputBinSized(CType, new OutputAttribute("Output"));
            }
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected)
            {
                if (pd.OutputPins.ContainsKey("Output"))
                {
                    pd.OutputPins["Output"].Spread.SliceCount = FInput.Pin.SliceCount;
                    for (int i = 0; i < FInput.Pin.SliceCount; i++)
                    {
                        var cspread = (NGISpread) pd.OutputPins["Output"].Spread[i];
                        if (CType == FInput[i].GetType())
                        {
                            cspread.SliceCount = 1;
                            cspread[0] = FInput[i];
                        }
                        else
                        {
                            cspread.SliceCount = 0;
                        }
                    }
                }
            }
            else
            {
                if (pd.OutputPins.ContainsKey("Output"))
                {
                    pd.OutputPins["Output"].Spread.SliceCount = 0;
                }
            }
        }
    }

    [PluginInfo(
         Name = "Expand",
         Category = "Node",
         Tags = "Split",
         Author = "microdee",
         AutoEvaluate = true)]
    public class ObjectExpandNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] protected IPluginHost2 FPluginHost; 
        [Import] protected IIOFactory FIOFactory;

        [Config("Type", DefaultString = "")] public IDiffSpread<string> FType;
        [Input("Force Update", Order = 100)] public ISpread<bool> FForceUpdate;

        protected override void PreInitialize()
        {
            ConfigPinCopy = FType;
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

        protected Dictionary<PropertyInfo, bool> IsPropertyEnumerable = new Dictionary<PropertyInfo, bool>();
        protected Dictionary<FieldInfo, bool> IsFieldEnumerable = new Dictionary<FieldInfo, bool>();

        protected override void OnConfigPinChanged()
        {
            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, "");
                break;
            }
            if (IsConfigDefault()) return;
            Pd.RemoveAllInput();
            Pd.RemoveAllOutput();
            IsPropertyEnumerable.Clear();
            IsFieldEnumerable.Clear();
            CType = Type.GetType(FType[0]);
            if(CType == null) return;

            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, CType.GetCSharpName());
                break;
            }

            Pd.AddInput(CType, new InputAttribute("Input"));
            foreach (var field in CType.GetFields())
            {
                if (field.IsStatic) continue;
                if (field.FieldType.IsPointer) continue;
                if (!field.IsPublic) continue;
                var enumerable = false;
                if ((field.FieldType.GetInterface("IEnumerable") != null) && (field.FieldType != typeof(string)))
                {
                    try
                    {
                        var interfaces = field.FieldType.GetInterfaces().ToList();
                        interfaces.Add(field.FieldType);
                        var stype = interfaces
                            .Where(type =>
                            {
                                try
                                {
                                    var res = type.GetGenericTypeDefinition();
                                    if (res == null) return false;
                                    return res == typeof(IEnumerable<>);
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            })
                            .ToArray()[0];
                        Pd.AddOutputBinSized(field.FieldType.GetGenericArguments()[0], new OutputAttribute(field.Name));
                        enumerable = true;
                    }
                    catch (Exception)
                    {
                        Pd.AddOutput(field.FieldType, new OutputAttribute(field.Name));
                        enumerable = false;
                    }
                }
                else
                {
                    Pd.AddOutput(field.FieldType, new OutputAttribute(field.Name));
                    enumerable = false;
                }
                IsFieldEnumerable.Add(field, enumerable);
            }
            foreach (var prop in CType.GetProperties())
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                var enumerable = false;
                if ((prop.PropertyType.GetInterface("IEnumerable") != null) && (prop.PropertyType != typeof(string)))
                {
                    try
                    {
                        var interfaces = prop.PropertyType.GetInterfaces().ToList();
                        interfaces.Add(prop.PropertyType);
                        var stype = interfaces
                            .Where(type =>
                            {
                                try
                                {
                                    var res = type.GetGenericTypeDefinition();
                                    if (res == null) return false;
                                    return res == typeof(IEnumerable<>);
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            })
                            .ToArray()[0].GenericTypeArguments[0];
                        Pd.AddOutputBinSized(stype, new OutputAttribute(prop.Name));
                        enumerable = true;
                    }
                    catch (Exception)
                    {
                        Pd.AddOutput(prop.PropertyType, new OutputAttribute(prop.Name));
                        enumerable = false;
                    }
                }
                else
                {
                    Pd.AddOutput(prop.PropertyType, new OutputAttribute(prop.Name));
                    enumerable = false;
                }
                IsPropertyEnumerable.Add(prop, enumerable);
            }
        }

        protected Type CType;
        protected int InitDescSet = 0;
        protected PinDictionary Pd;

        public void Evaluate(int SpreadMax)                                                               
        {
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
            if(!Pd.InputPins.ContainsKey("Input")) return;
            if (Pd.InputPins["Input"].Spread.SliceCount == 0)
            {
                foreach (var outpin in Pd.OutputPins.Values)
                {
                    outpin.Spread.SliceCount = 0;
                }
                return;
            }
            if (Pd.InputPins["Input"].Spread[0] == null) return;
            bool changed = Pd.InputPins["Input"].Spread.IsChanged;
            var sprmax = Pd.InputPins["Input"].Spread.SliceCount;
            if (changed || FType.IsChanged || FForceUpdate[0])
            {
                foreach (var pin in Pd.OutputPins.Values)
                {
                    pin.Spread.SliceCount = sprmax;
                }
                for (int i = 0; i < sprmax; i++)
                {
                    var obj = Pd.InputPins["Input"].Spread[i];
                    if(obj == null) continue;
                    foreach (var field in IsFieldEnumerable.Keys)
                    {
                        if (IsFieldEnumerable[field])
                        {
                            var enumerable = (IEnumerable)field.GetValue(obj);
                            var spread = (NGISpread)Pd.OutputPins[field.Name].Spread[i];
                            spread.SliceCount = 0;
                            foreach (var o in enumerable)
                            {
                                spread.SliceCount++;
                                spread[-1] = o;
                            }
                        }
                        else
                        {
                            Pd.OutputPins[field.Name].Spread[i] = field.GetValue(obj);
                        }
                    }
                    foreach (var prop in IsPropertyEnumerable.Keys)
                    {
                        if(IsPropertyEnumerable[prop])
                        {
                            var enumerable = (IEnumerable)prop.GetValue(obj);
                            var spread = (NGISpread)Pd.OutputPins[prop.Name].Spread[i];
                            spread.SliceCount = 0;
                            foreach (var o in enumerable)
                            {
                                spread.SliceCount++;
                                spread[-1] = o;
                            }
                        }
                        else
                        {
                            Pd.OutputPins[prop.Name].Spread[i] = prop.GetValue(obj);
                        }
                    }
                }
            }
        }
    }
}