using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.Reflection;

namespace VVVV.Nodes.PDDN
{
    public static class ObjectHelper
    {
        public static void DisposeDisposable(object obj)
        {
            if (!(obj is IDisposable)) return;
            var t = (IDisposable) obj;
            t.Dispose();
        }

        public static Type ForceGetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
    public abstract class ConstructorNode<T> : IPluginEvaluate
    {
        [Input("Construct", IsBang = true, Order = 0)]
        public ISpread<bool> FConstruct;
        [Input("Auto Clear", DefaultBoolean = true, Order = 1)]
        public ISpread<bool> FAutoClear;
        [Output("Output Object", Order = 0)]
        public ISpread<T> FOutput;
        
        public int CurrObj;
        public int SliceCount = 0;
        public int fc = 0;
        public virtual void SetSliceCount(int SpreadMax)
        {
            this.SliceCount = SpreadMax;
        }
        public virtual void InitializeFrame()
        { }

        public abstract T ConstructObject();

        public void Evaluate(int SpreadMax)
        {
            this.SetSliceCount(SpreadMax);
            this.InitializeFrame();

            if (FAutoClear[0])
            {
                bool clear = false;
                for (int i = 0; i < this.SliceCount; i++)
                {
                    if (FConstruct[i]) clear = true;
                }
                if (clear) fc = 0;
            }
            if (fc == 0) FOutput.SliceCount = 0;
            fc++;

            for (int i = 0; i < this.SliceCount; i++)
            {
                this.CurrObj = i;
                if (FConstruct[i])
                {
                    var ro = ConstructObject();
                    if (ro != null) FOutput.Add(ro);
                }
            }
        }
    }
    public abstract class ConstructAndSetNode<T> : IPluginEvaluate
    {
        [Input("Construct", IsBang = true, Order = 0)]
        public ISpread<bool> FConstruct;
        [Input("Auto Clear", DefaultBoolean = true, Order = 1)]
        public ISpread<bool> FAutoClear;
        [Input("Set", IsBang = true, Order = 2)]
        public ISpread<bool> FSet;
        [Input("Dispose Disposable", Order = 3, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<bool> FDisposeDisposable;
        [Output("Output Object", Order = 0)]
        public ISpread<T> FOutput;
        
        public int CurrObj;
        public int SliceCount = 0;
        public int fc = 0;
        public virtual void SetSliceCount(int SpreadMax)
        {
            SliceCount = SpreadMax;
        }
        public virtual void InitializeFrame() { }

        public abstract T ConstructObject();

        public virtual void SetObject() { }

        public void Evaluate(int SpreadMax)
        {
            this.SetSliceCount(SpreadMax);
            this.InitializeFrame();

            if (FAutoClear[0])
            {
                bool clear = false;
                for (int i = 0; i < this.SliceCount; i++)
                {
                    if (FConstruct[i]) clear = true;
                }
                if (clear) fc = 0;
            }
            if (fc == 0)
            {
                for (int i = 0; i < FOutput.SliceCount; i++)
                {
                    if ((FOutput[i] != null) && FDisposeDisposable[0])
                        ObjectHelper.DisposeDisposable(FOutput[i]);
                }
                FOutput.SliceCount = 0;
            }
            fc++;

            bool empty = FOutput.SliceCount == 0;
            for (int i = 0; i < this.SliceCount; i++)
            {
                this.CurrObj = i;
                if (FConstruct[i] || (FSet[i] && empty))
                {
                    var ro = ConstructObject();
                    if (ro != null) FOutput.Add(ro);
                }
            }
            for (int i = 0; i < FOutput.SliceCount; i++)
            {
                if (!FSet[i]) continue;
                CurrObj = i;
                SetObject();
            }
        }
    }

    public abstract class ObjectSplitNode<T> : IPartImportsSatisfiedNotification, IPluginEvaluate
    {
        [Input("Input")] public Pin<T> FInput;
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        protected bool ExposePrivate = false;

        public virtual void OnImportsSatisfiedBegin() { }
        public virtual void OnImportsSatisfiedEnd() { }

        public virtual void OnEvaluateBegin() { }
        public virtual void OnEvaluateEnd() { }

        public virtual void OnChangedBegin() { }
        public virtual void OnChangedEnd() { }

        public virtual object TransformOutput(object obj, MemberInfo member, int i)
        {
            return obj;
        }

        public virtual Type TransformType(Type original, MemberInfo member)
        {
            return original;
        }

        protected Dictionary<MemberInfo, bool> IsMemberEnumerable = new Dictionary<MemberInfo, bool>();

        protected Type CType;
        protected PinDictionary Pd;

        private void AddMemberPin(MemberInfo member)
        {
            if(!(member is FieldInfo) && !(member is PropertyInfo)) return;
            Type memberType = typeof(object);
            switch (member)
            {
                case FieldInfo field:
                    if (field.IsStatic) return;
                    if (field.FieldType.IsPointer) return;
                    if (!field.FieldType.IsPublic && !ExposePrivate) return;

                    memberType = field.FieldType;
                    break;
                case PropertyInfo prop:
                    if(!prop.CanRead) return;
                    if(prop.GetIndexParameters().Length > 0) return;

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

        public void OnImportsSatisfied()
        {
            Pd = new PinDictionary(FIOFactory);
            CType = typeof(T);

            OnImportsSatisfiedBegin();

            foreach (var field in CType.GetFields())
                AddMemberPin(field);
            foreach (var prop in CType.GetProperties())
                AddMemberPin(prop);

            OnImportsSatisfiedEnd();
        }

        public void Evaluate(int SpreadMax)
        {
            OnEvaluateBegin();
            if (FInput.SliceCount == 0)
            {
                foreach (var outpin in Pd.OutputPins.Values)
                {
                    outpin.Spread.SliceCount = 0;
                }
                OnEvaluateEnd();
                return;
            }

            if (FInput[0] == null)
            {
                OnEvaluateEnd();
                return;
            }
            var sprmax = FInput.SliceCount;
            if (FInput.IsChanged)
            {
                OnChangedBegin();
                foreach (var pin in Pd.OutputPins.Values)
                {
                    pin.Spread.SliceCount = sprmax;
                }
                for (int i = 0; i < sprmax; i++)
                {
                    var obj = FInput[i];
                    if (obj == null) continue;
                    foreach (var member in IsMemberEnumerable.Keys)
                        AssignMemberValue(member, obj, i);
                }
                OnChangedEnd();
            }
            OnEvaluateEnd();
        }
    }
}
