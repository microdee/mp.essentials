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

        public void OnImportsSatisfied()
        {
            Pd = new PinDictionary(FIOFactory);
            CType = typeof(T);

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

        protected Dictionary<PropertyInfo, bool> IsPropertyEnumerable = new Dictionary<PropertyInfo, bool>();
        protected Dictionary<FieldInfo, bool> IsFieldEnumerable = new Dictionary<FieldInfo, bool>();

        protected Type CType;
        protected PinDictionary Pd;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.SliceCount == 0)
            {
                foreach (var outpin in Pd.OutputPins.Values)
                {
                    outpin.Spread.SliceCount = 0;
                }
                return;
            }
            if (FInput[0] == null) return;
            var sprmax = FInput.SliceCount;
            if (FInput.IsChanged)
            {
                foreach (var pin in Pd.OutputPins.Values)
                {
                    pin.Spread.SliceCount = sprmax;
                }
                for (int i = 0; i < sprmax; i++)
                {
                    var obj = FInput[i];
                    if (obj == null) continue;
                    foreach (var field in IsFieldEnumerable.Keys)
                    {
                        if (IsFieldEnumerable[field])
                        {
                            var enumerable = (IEnumerable)field.GetValue(obj);
                            var spread = (ISpread)Pd.OutputPins[field.Name].Spread[i];
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
                        if (IsPropertyEnumerable[prop])
                        {
                            var enumerable = (IEnumerable)prop.GetValue(obj);
                            var spread = (ISpread)Pd.OutputPins[prop.Name].Spread[i];
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
