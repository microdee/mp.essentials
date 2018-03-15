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
using mp.pddn;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.Reflection;
using VVVV.Utils.VMath;
using Matrix4x4 = System.Numerics.Matrix4x4;

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
        [Output("Is Null")] public ISpread<bool> FNull;

        public void OnImportsSatisfied()
        {
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input"));
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected)
            {
                FType.SliceCount = FNull.SliceCount = FInput.Pin.SliceCount;
                for (int i = 0; i < FInput.Pin.SliceCount; i++)
                {
                    if (FInput[i] == null)
                    {
                        FType[i] = "null";
                        FNull[i] = true;
                    }
                    else
                    {
                        FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                        FNull[i] = false;
                    }
                }
            }
            else
            {
                FType.SliceCount = FNull.SliceCount = 0;
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
                    if (FInput[i] == null)
                    {
                        FType[i].SliceCount = 0;
                        continue;
                    }
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

        [Output("Object Type")] public ISpread<string> FType;
        [Output("Is Null")] public ISpread<bool> FNull;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsConnected)
            {
                FType.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if (FInput[i] == null)
                    {
                        FType[i] = "null";
                        FNull[i] = true;
                    }
                    else
                    {
                        FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                        FNull[i] = false;
                    }
                }
            }
            else
            {
                FType.SliceCount = 0;
            }
        }
    }

    /*
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

        private Type _pg.GroupType;

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void Initialize()
        {
            if (FType[0] != "")
            {
                _pg.GroupType = Type.GetType(FType[0], true);

                if (FTypeRef.Pin.SliceCount != 0)
                {
                    if (FTypeRef[0] != null)
                    {
                        Type T = FTypeRef[0].GetType();
                        if (T != _pg.GroupType)
                        {
                            pd.RemoveAllOutput();
                            pd.AddOutputBinSized(T, new OutputAttribute("Output"));
                            _pg.GroupType = T;
                            FType[0] = T.AssemblyQualifiedName;
                        }
                    }
                }

                //RemoveAllOutput();
                pd.AddOutputBinSized(_pg.GroupType, new OutputAttribute("Output"));
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
                    if (_pg.GroupType == null)
                        valid = true;
                    else
                        valid = T != _pg.GroupType;
                    if (valid)
                    {
                        pd.RemoveAllOutput();
                        pd.AddOutputBinSized(T, new OutputAttribute("Output"));
                        _pg.GroupType = T;
                        FType[0] = T.AssemblyQualifiedName;
                    }
                }
                if (pd.OutputPins.ContainsKey("Output"))
                {
                    pd.OutputPins["Output"].Spread.SliceCount = FInput.Pin.SliceCount;
                    for (int i = 0; i < FInput.Pin.SliceCount; i++)
                    {
                        var cspread = (ISpread) pd.OutputPins["Output"].Spread[i];
                        if (_pg.GroupType == FInput[i].GetType())
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

        private Type _pg.GroupType;

        protected override void OnConfigPinChanged()
        {
            _pg.GroupType = Type.GetType(FType[0], true);
            pd.RemoveAllOutput();
            pd.AddOutputBinSized(_pg.GroupType, new OutputAttribute("Output"));
        }

        protected override bool IsConfigDefault()
        {
            return FType[0] == "";
        }

        protected override void Initialize()
        {
            if (FType[0] != "")
            {
                _pg.GroupType = Type.GetType(FType[0], true);
                //RemoveAllOutput();
                pd.AddOutputBinSized(_pg.GroupType, new OutputAttribute("Output"));
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
                        var cspread = (ISpread) pd.OutputPins["Output"].Spread[i];
                        if (_pg.GroupType == FInput[i].GetType())
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
    */

    [PluginInfo(
         Name = "Expand",
         Category = "Node",
         Tags = "Split",
         Author = "microdee",
         AutoEvaluate = true)]
    public class ObjectExpandNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;
        [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
        private bool _typeChanged;
        private bool _pgready;
        private DiffSpreadPin _input;

        [Input("Force Update", Order = 100)] public ISpread<bool> FForceUpdate;
        [Input("Expose private", Order = 101)] public ISpread<bool> FExposePrivate;

        public Type TransformType(Type original, MemberInfo member)
        {
            return MiscExtensions.MapSystemNumericsTypeToVVVV(original);
        }

        public object TransformOutput(object obj, MemberInfo member, int i)
        {
            return MiscExtensions.MapSystemNumericsValueToVVVV(obj);
        }

        public void OnImportsSatisfied()
        {
            Pd = new PinDictionary(FIOFactory);
            foreach (var pin in FPluginHost.GetPins())
            {
                if (pin.Name != "Descriptive Name") continue;
                pin.SetSlice(0, "");
                break;
            }

            _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input", 100);
            _pg.OnTypeChangeEnd += (sender, args) =>
            {
                _typeChanged = true;

                foreach (var pin in FPluginHost.GetPins())
                {
                    if (pin.Name != "Descriptive Name") continue;
                    pin.SetSlice(0, "");
                    break;
                }
                Pd.RemoveAllInput();
                IsMemberEnumerable.Clear();

                foreach (var pin in FPluginHost.GetPins())
                {
                    if (pin.Name != "Descriptive Name") continue;
                    pin.SetSlice(0, _pg.GroupType.GetCSharpName());
                    break;
                }
                
                foreach (var field in _pg.GroupType.GetFields())
                    AddMemberPin(field);
                foreach (var prop in _pg.GroupType.GetProperties())
                    AddMemberPin(prop);

                if (_pgready) return;
                _pgready = true;
                _input = _pg.AddInput(new InputAttribute("Input"));
            };
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
                    Pd.AddOutput(TransformType(stype, member), new OutputAttribute(member.Name), binSized: true);
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
                var spread = (ISpread)Pd.OutputPins[member.Name].Spread[i];
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
        
        protected int InitDescSet = 0;
        protected PinDictionary Pd;

        public void Evaluate(int SpreadMax)
        {
            var typechanged = false;
            if (_typeChanged)
            {
                typechanged = true;
                _typeChanged = false;
            }
            if (InitDescSet < 5)
            {
                foreach (var pin in FPluginHost.GetPins())
                {
                    if (pin.Name != "Descriptive Name") continue;
                    pin.SetSlice(0, _pg.GroupType?.GetCSharpName() ?? "");
                    InitDescSet++;
                    break;
                }
            }
            if (!_pgready) return;
            if (_input.Spread.SliceCount == 0)
            {
                foreach (var outpin in Pd.OutputPins.Values)
                {
                    outpin.Spread.SliceCount = 0;
                }
                return;
            }
            if (_input[0] == null) return;
            bool changed = _input.Spread.IsChanged;
            var sprmax = _input.Spread.SliceCount;
            if (changed || typechanged || FForceUpdate[0])
            {
                foreach (var pin in Pd.OutputPins.Values)
                {
                    pin.Spread.SliceCount = sprmax;
                }
                for (int i = 0; i < sprmax; i++)
                {
                    var obj = _input.Spread[i];
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