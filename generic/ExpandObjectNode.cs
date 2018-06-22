using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.Reflection;

namespace mp.essentials.Nodes.Generic
{
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
                    if (prop.PropertyType.IsPointer) return;
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
                    if (obj == null) continue;
                    foreach (var field in IsMemberEnumerable.Keys)
                        AssignMemberValue(field, obj, i);
                    foreach (var prop in IsMemberEnumerable.Keys)
                        AssignMemberValue(prop, obj, i);
                }
            }
        }
    }
}
