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
        Author = "microdee",
        Tags = "null, IsNull"
    )]
    public class ObjectGetTypeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] public IPluginHost2 FPluginHost;

        protected GenericInput FInput;

        [Output("Object Type")] public ISpread<string> FType;
        [Output("Is Null")] public ISpread<bool> FNull;
        [Output("Not Null")] public ISpread<bool> FNotNull;

        public void OnImportsSatisfied()
        {
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input"));
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected)
            {
                FType.SliceCount = FNull.SliceCount = FNotNull.SliceCount = FInput.Pin.SliceCount;
                for (int i = 0; i < FInput.Pin.SliceCount; i++)
                {
                    if (FInput[i] == null)
                    {
                        FType[i] = "null";
                        FNull[i] = true;
                        FNotNull[i] = false;
                    }
                    else
                    {
                        FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                        FNull[i] = false;
                        FNotNull[i] = true;
                    }
                }
            }
            else
            {
                FType.SliceCount = FNotNull.SliceCount = FNull.SliceCount = 0;
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
        [Output("Is Null")] public ISpread<bool> FNull;
        [Output("Not Null")] public ISpread<bool> FNotNull;

        public void OnImportsSatisfied()
        {
            FInput = new GenericInput(FPluginHost, new InputAttribute("Input"));
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.Connected)
            {
                FType.SliceCount = FNull.SliceCount = FNotNull.SliceCount = FInput.Pin.SliceCount;
                for (int i = 0; i < FInput.Pin.SliceCount; i++)
                {
                    if (FInput[i] == null)
                    {
                        FType[i].SliceCount = 0;
                        FNull[i] = true;
                        FNotNull[i] = false;
                        continue;
                    }
                    FNull[i] = false;
                    FNotNull[i] = true;
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
                FType.SliceCount = FNotNull.SliceCount = FNull.SliceCount = 0;
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
        [Output("Not Null")] public ISpread<bool> FNotNull;

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsConnected)
            {
                FType.SliceCount = FNull.SliceCount = FNotNull.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if (FInput[i] == null)
                    {
                        FType[i] = "null";
                        FNull[i] = true;
                        FNotNull[i] = false;
                    }
                    else
                    {
                        FType[i] = FInput[i].GetType().AssemblyQualifiedName;
                        FNull[i] = false;
                        FNotNull[i] = true;
                    }
                }
            }
            else
            {
                FType.SliceCount = FNull.SliceCount = FNotNull.SliceCount = 0;
            }
        }
    }
}