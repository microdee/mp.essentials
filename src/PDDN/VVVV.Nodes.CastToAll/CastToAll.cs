using System;
using System.Collections.Generic;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;

namespace VVVV.Nodes
{

    public static class TypeUtils
    {
        public static IEnumerable<Type> GetTypes(this Type type)
        {
            // is there any base type?
            if ((type == null) || (type.BaseType == null))
            {
                yield break;
            }
            yield return type;
            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        public static string GetName(this Type T, bool full)
        {
            if (full) return T.FullName;
            else return T.Name;
        }
    }

    [PluginInfo(Name = "Cast", Category = "Node", AutoEvaluate = true)]
    public class CastToAllInheritedNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import]
        protected IIOFactory FIOFactory;

        [Import]
        public IPluginHost2 FPluginHost;
        [Import]
        public IHDEHost FHDEHost;

        [Config("Type", DefaultString = "")]
        public IDiffSpread<string> FType;

        public GenericInput FInput;
        public PinDictionary PinDictionary;
        
        private Type CType;
        private string CTypeS;

        private bool Initialized = false;

        private Type FindHighestType()
        {
            Type type = FInput[0].GetType();
            if (FInput.Pin.SliceCount > 1)
            {
                object po = FInput[0];
                for (int i = 1; i < FInput.Pin.SliceCount; i++)
                {
                    if (!type.IsInstanceOfType(FInput[i]))
                    {
                        foreach (var T in FInput[i].GetType().GetTypes())
                        {
                            if (T.IsInstanceOfType(po))
                            {
                                type = T;
                                break;
                            }
                        }
                    }
                    po = FInput[i];
                }
            }
            return type;
        }

        protected void Init()
        {
            if (!Initialized)
            {
                CType = Type.GetType(FType[0], true);

                if (FInput.Pin.SliceCount != 0)
                {
                    if (FInput[0] != null)
                    {
                        Type T = FindHighestType();
                        if (T != CType)
                        {
                            PinDictionary.RemoveAllOutput();
                            PinDictionary.AddOutput(T, new OutputAttribute("Output"));
                            CType = T;
                            CTypeS = T.AssemblyQualifiedName;
                            FType[0] = CTypeS;
                        }
                    }
                }

                //RemoveAllOutput();
                PinDictionary.AddOutput(CType, new OutputAttribute("Output"));
                if(CType != null)
                    CTypeS = CType.AssemblyQualifiedName;
                Initialized = true;
            }
        }

        protected void Write()
        {
            int sc = FInput.Pin.SliceCount;
            PinDictionary.OutputPins["Output"].Spread.SliceCount = sc;

            for (int i = 0; i < sc; i++)
            {
                PinDictionary.OutputPins["Output"].Spread[i] = FInput[i];
            }
        }
        public void OnImportsSatisfied()
        {
            PinDictionary = new PinDictionary(FIOFactory);
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input"));
            FInput.Pin.Order = 0;
            FType.Changed += OnSavedTypeChanged;
        }

        private void OnSavedTypeChanged(IDiffSpread<string> spread)
        {
            if(FType[0] != "")
                Init();
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Pin.SliceCount != 0)
            {
                if (FInput[0] != null)
                {
                    Type T = FindHighestType();
                    if(T != CType)
                    {
                        PinDictionary.RemoveAllOutput();
                        PinDictionary.AddOutput(T, new OutputAttribute("Output"));
                        CType = T;
                        CTypeS = T.AssemblyQualifiedName;
                        FType[0] = CTypeS;
                    }
                    if (PinDictionary.OutputPins.ContainsKey("Output"))
                        Write();
                }
            }
            else
            {
                if(PinDictionary.OutputPins.ContainsKey("Output"))
                    PinDictionary.OutputPins["Output"].Spread.SliceCount = 0;
            }
        }
    }
}
