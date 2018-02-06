using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Numerics;
using System.Reflection;
using md.stdl.Mathematics;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.PDDN;
using VVVV.Utils.Reflection;
using VVVV.Utils.VMath;
using Matrix4x4 = System.Numerics.Matrix4x4;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace mp.essentials.Nodes.Generic
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
         Name = "GetAllTypes",
         Category = "Object",
         Author = "microdee"
     )]
    public class ObjectGetAllTypesNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        public IPluginHost2 FPluginHost;

        protected GenericInput FInput;

        [Input("Inheritence Level", DefaultValue = -1, Order = 2)]
        public ISpread<int> FInhLevel;
        [Output("Object Type")]
        public ISpread<ISpread<string>> FType;

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
                    var types = FInput[i].GetType().GetTypes().ToArray();
                    if (FInhLevel[i] < 0)
                    {
                        FType[i].SliceCount = types.Length;
                        for (int j = 0; j < types.Length; j++)
                        {
                            FType[i][j] = types[j].AssemblyQualifiedName;
                        }
                    }
                    else
                    {
                        FType[i].SliceCount = 1;
                        FType[i][0] = types[Math.Min(FInhLevel[i], types.Length-1)].AssemblyQualifiedName;
                    }
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
        public GenericInput FRefType;
        [Input("Expose private", Order = 101)] public ISpread<bool> FExposePrivate;
        [Input("Learnt Type Inheritence Level", Order = 102, Visibility = PinVisibility.Hidden, DefaultValue = 0)]
        public ISpread<int> FTypeInheritence;
        [Input("Learn Type", Order = 103, IsBang = true)] public ISpread<bool> FLearnType;

        public Type TransformType(Type original, MemberInfo member)
        {
            if (original == typeof(Vector2))
            {
                return typeof(Vector2D);
            }
            if (original == typeof(Vector3))
            {
                return typeof(Vector3D);
            }
            if (original == typeof(Vector4))
            {
                return typeof(Vector4D);
            }
            if (original == typeof(Matrix4x4))
            {
                return typeof(VVVV.Utils.VMath.Matrix4x4);
            }
            return original;
        }

        public object TransformOutput(object obj, MemberInfo member, int i)
        {
            switch (obj)
            {
                case Vector2 v:
                {
                    return v.AsVVector();
                }
                case Vector3 v:
                {
                    return v.AsVVector();
                }
                case Vector4 v:
                {
                    return v.AsVVector();
                }
                case Matrix4x4 v:
                {
                    return v.AsVMatrix4X4();
                }
                default:
                {
                    return obj;
                }
            }
        }

        protected override void PreInitialize()
        {
            ConfigPinCopy = FType;
            FRefType = new GenericInput(FPluginHost, new InputAttribute("Reference Type")
            {
                Order = 101
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

        protected Dictionary<MemberInfo, bool> IsMemberEnumerable = new Dictionary<MemberInfo, bool>();

        private void AddMemberPin(MemberInfo member)
        {
            if (!(member is FieldInfo) && !(member is PropertyInfo)) return;
            Type memberType = typeof(object);
            switch (member)
            {
                case FieldInfo field:
                    if (field.IsStatic) return;
                    if (field.FieldType.IsPointer) return;
                    if (!field.FieldType.IsPublic && !FExposePrivate[0]) return;

                    memberType = field.FieldType;
                    break;
                case PropertyInfo prop:
                    if (!prop.CanRead) return;
                    if (prop.GetIndexParameters().Length > 0) return;

                    memberType = prop.PropertyType;
                    break;
            }
            var enumerable = false;
            if ((memberType.GetInterface("IEnumerable") != null) && (memberType != typeof(string)))
            {
                try
                {
                    var interfaces = memberType.GetInterfaces().ToList();
                    interfaces.Add(memberType);
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
                        .First().GenericTypeArguments[0];
                    Pd.AddOutputBinSized(TransformType(stype, member), new OutputAttribute(member.Name));
                    enumerable = true;
                }
                catch (Exception)
                {
                    Pd.AddOutput(TransformType(memberType, member), new OutputAttribute(member.Name));
                    enumerable = false;
                }
            }
            else
            {
                Pd.AddOutput(TransformType(memberType, member), new OutputAttribute(member.Name));
                enumerable = false;
            }
            IsMemberEnumerable.Add(member, enumerable);
        }

        private void AssignMemberValue(MemberInfo member, object input, int i)
        {
            object memberValue = null;
            switch (member)
            {
                case FieldInfo field:
                    memberValue = field.GetValue(input);
                    break;
                case PropertyInfo prop:
                    memberValue = prop.GetValue(input);
                    break;
            }
            if (IsMemberEnumerable[member])
            {
                var enumerable = (IEnumerable)memberValue;
                var spread = (NGISpread)Pd.OutputPins[member.Name].Spread[i];
                spread.SliceCount = 0;
                foreach (var o in enumerable)
                {
                    spread.SliceCount++;
                    spread[-1] = TransformOutput(o, member, i);
                }
            }
            else
            {
                Pd.OutputPins[member.Name].Spread[i] = TransformOutput(memberValue, member, i);
            }
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
            IsMemberEnumerable.Clear();
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
                AddMemberPin(field);
            foreach (var prop in CType.GetProperties())
                AddMemberPin(prop);
        }

        protected Type CType;
        protected int InitDescSet = 0;
        protected PinDictionary Pd;

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
            if (IsConfigDefault()) return;
            if (!Pd.InputPins.ContainsKey("Input")) return;
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
                    foreach (var field in IsMemberEnumerable.Keys)
                        AssignMemberValue(field, obj, i);
                    foreach (var prop in IsMemberEnumerable.Keys)
                        AssignMemberValue(prop, obj, i);
                }
            }
        }
    }
}