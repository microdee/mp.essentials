using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;
using NGISpread = VVVV.PluginInterfaces.V2.NonGeneric.ISpread;
using NGIDiffSpread = VVVV.PluginInterfaces.V2.NonGeneric.IDiffSpread;

namespace VVVV.Nodes.PDDN
{
    public abstract class SimplePin
    {
        public Type Type;
        public IOAttribute Attributes;
        public IIOContainer IOContainer;
    }
    public class SpreadPin : SimplePin
    {
        public NGISpread Spread;
        public SpreadPin(NGISpread spread, IOAttribute attr, IIOContainer ioc)
        {
            Attributes = attr;
            IOContainer = ioc;
            Spread = spread;
        }
    }
    public class DiffSpreadPin : SimplePin
    {
        public NGIDiffSpread Spread;
        public DiffSpreadPin(NGIDiffSpread spread, IOAttribute attr, IIOContainer ioc)
        {
            Attributes = attr;
            IOContainer = ioc;
            Spread = spread;
        }
    }
    public static class NodeUtils
    {
        public static NGISpread ToISpread(this IIOContainer pin)
        {
            return (NGISpread)(pin.RawIOObject);
        }

        public static NGIDiffSpread ToIDiffSpread(this IIOContainer pin)
        {
            return (NGIDiffSpread)(pin.RawIOObject);
        }
        public static ISpread<T> ToGenericISpread<T>(this IIOContainer pin)
        {
            return (ISpread<T>)(pin.RawIOObject);
        }
    }
    public class PinDictionary
    {
        protected IIOFactory FIOFactory;

        public PinDictionary(IIOFactory iofactory)
        {
            FIOFactory = iofactory;
        }

        public Dictionary<string, DiffSpreadPin> InputPins = new Dictionary<string, DiffSpreadPin>();
        public Dictionary<string, SpreadPin> OutputPins = new Dictionary<string, SpreadPin>();
        public List<string> InputTaggedForRemove = new List<string>();
        public List<string> OutputTaggedForRemove = new List<string>();

        public void AddInput(Type T, InputAttribute attr)
        {
            if (!InputPins.ContainsKey(attr.Name))
            {
                Type pinType = typeof (IDiffSpread<>).MakeGenericType(T);
                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToIDiffSpread();
                var pin = new DiffSpreadPin(ispread, attr, ioc);
                InputPins.Add(attr.Name, pin);
                pin.Type = T;
            }
        }
        public void AddInputBinSized(Type T, InputAttribute attr)
        {
            if (!InputPins.ContainsKey(attr.Name))
            {
                Type pinType = typeof (IDiffSpread<>).MakeGenericType(typeof (ISpread<>).MakeGenericType(T));
                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToIDiffSpread();
                var pin = new DiffSpreadPin(ispread, attr, ioc);
                InputPins.Add(attr.Name, pin);
                pin.Type = T;
            }
        }
        public void ChangeInputType(string name, Type T, bool BinSizable = false)
        {
            if (InputPins.ContainsKey(name))
            {
                var pin = InputPins[name];
                pin.IOContainer.Dispose();
                var attr = pin.Attributes;

                Type pinType;
                if (BinSizable)
                    pinType = typeof(IDiffSpread<>).MakeGenericType(typeof(ISpread<>).MakeGenericType(T));
                else
                    pinType = typeof(IDiffSpread<>).MakeGenericType(T);

                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToIDiffSpread();
                pin.IOContainer = ioc;
                pin.Spread = ispread;
                pin.Type = T;
            }
        }
        public void AddOutput(Type T, OutputAttribute attr)
        {
            if (!OutputPins.ContainsKey(attr.Name))
            {
                Type pinType = typeof (ISpread<>).MakeGenericType(T);
                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToISpread();
                var pin = new SpreadPin(ispread, attr, ioc);
                OutputPins.Add(attr.Name, pin);
                pin.Type = T;
            }
        }
        public void AddOutputBinSized(Type T, OutputAttribute attr)
        {
            if (!OutputPins.ContainsKey(attr.Name))
            {
                Type pinType = typeof (ISpread<>).MakeGenericType(typeof (ISpread<>).MakeGenericType(T));
                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToISpread();
                var pin = new SpreadPin(ispread, attr, ioc);
                OutputPins.Add(attr.Name, pin);
                pin.Type = T;
            }
        }
        public void ChangeOutputType(string name, Type T, bool BinSizable = false)
        {
            if (OutputPins.ContainsKey(name))
            {
                var pin = OutputPins[name];
                pin.IOContainer.Dispose();
                var attr = pin.Attributes;

                Type pinType;
                if (BinSizable)
                    pinType = typeof(ISpread<>).MakeGenericType(typeof(ISpread<>).MakeGenericType(T));
                else
                    pinType = typeof(ISpread<>).MakeGenericType(T);

                var ioc = FIOFactory.CreateIOContainer(pinType, attr);
                var ispread = ioc.ToIDiffSpread();
                pin.IOContainer = ioc;
                pin.Spread = ispread;
                pin.Type = T;
            }
        }
        public void RemoveInput(string name)
        {
            if (InputPins.ContainsKey(name))
            {
                InputPins[name].IOContainer.Dispose();
                InputPins.Remove(name);
            }
        }
        public void RemoveTaggedInput()
        {
            foreach (var k in InputTaggedForRemove)
            {
                RemoveInput(k);
            }
        }
        public void RemoveAllInput()
        {
            foreach (var p in InputPins.Keys)
            {
                InputPins[p].IOContainer.Dispose();
            }
            InputPins.Clear();
        }
        public void RemoveOutput(string name)
        {
            if (OutputPins.ContainsKey(name))
            {
                OutputPins[name].IOContainer.Dispose();
                OutputPins.Remove(name);
            }
        }
        public void RemoveTaggedOutput()
        {
            foreach (var k in OutputTaggedForRemove)
            {
                RemoveOutput(k);
            }
        }
        public void RemoveAllOutput()
        {
            foreach (var p in OutputPins.Keys)
            {
                OutputPins[p].IOContainer.Dispose();
            }
            OutputPins.Clear();
        }
    }
}
