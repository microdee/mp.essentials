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
        public Dictionary<NotuiContext, IEnumerable<NotuiElement>> Instances { get; set; } = new Dictionary<NotuiContext, IEnumerable<NotuiElement>>();
        public Dictionary<string, object> NodeSpecific { get; set; } = new Dictionary<string, object>();

        public void RemoveDeletedInstances()
        {
            var removables = (from contextElementPair in Instances
                where contextElementPair.Key.FlatElements.All(e => e.Id != contextElementPair.Value.First().Id)
                select contextElementPair.Key).ToArray();
            foreach (var context in removables)
            {
                Instances.Remove(context);
            }
        }

        public void AddOrUpdateInstance(NotuiContext context, ElementPrototype element)
        {
            if (Instances.ContainsKey(context))
            {
                Instances[context] = context.FlatElements.Where(e => e.Id == element.Id);
            }
            else
            {
                Instances.Add(context, context.FlatElements.Where(e => e.Id == element.Id));
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
