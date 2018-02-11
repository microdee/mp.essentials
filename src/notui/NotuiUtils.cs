using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;

namespace mp.essentials.notui
{
    public class VEnvironmentData : AuxiliaryObject
    {
        public Dictionary<NotuiContext, NotuiElement> Instances { get; set; } = new Dictionary<NotuiContext, NotuiElement>();
        public Dictionary<string, object> NodeSpecific { get; set; } = new Dictionary<string, object>();

        public void RemoveDeletedInstances()
        {
            var removables = (from contextElementPair in Instances
                where !contextElementPair.Key.FlatElementList.ContainsKey(contextElementPair.Value.Id)
                select contextElementPair.Key).ToArray();
            foreach (var context in removables)
            {
                Instances.Remove(context);
            }
        }

        public void AddOrUpdateInstance(NotuiContext context, NotuiElement element)
        {
            if (Instances.ContainsKey(context))
            {
                Instances[context] = element;
            }
            else
            {
                Instances.Add(context, element);
            }
        }

        public override AuxiliaryObject Copy()
        {
            return new VEnvironmentData();
        }

        public override void UpdateFrom(AuxiliaryObject other)
        {
            if (other is VEnvironmentData venvdat)
            {
                Instances = venvdat.Instances;
                NodeSpecific = venvdat.NodeSpecific;
            }
        }
    }

    public static class NotuiUtils
    {
        public static void AttachManagementObject(this NotuiElement element, string nodepath, object obj)
        {
            if (element.EnvironmentObject == null)
                element.EnvironmentObject = new VEnvironmentData();
            if (element.EnvironmentObject is VEnvironmentData venvdat)
            {
                if (venvdat.NodeSpecific.ContainsKey(nodepath))
                {
                    venvdat.NodeSpecific[nodepath] = obj;
                }
                else
                {
                    venvdat.NodeSpecific.Add(nodepath, obj);
                }
            }
        }

        public static void AttachSliceId(this NotuiElement element, string nodepath, int id)
        {
            element.AttachManagementObject(nodepath, id);
        }
    }
}
