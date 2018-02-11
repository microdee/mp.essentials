using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.notui
{

    [PluginInfo(
        Name = "Rectangle",
        Category = "Notui.ElementPrototype",
        Version = "Join",
        Author = "microdee"
    )]
    public class RectangleElementNode : AbstractElementNode<RectangleElementPrototype>
    {
        protected override RectangleElementPrototype ConstructPrototype(int i, string id)
        {
            return new RectangleElementPrototype(string.IsNullOrWhiteSpace(id) ? null : id);
        }
    }

    [PluginInfo(
        Name = "Circle",
        Category = "Notui.ElementPrototype",
        Version = "Join",
        Author = "microdee"
    )]
    public class CircleElementNode : AbstractElementNode<CircleElementPrototype>
    {
        protected override CircleElementPrototype ConstructPrototype(int i, string id)
        {
            return new CircleElementPrototype(string.IsNullOrWhiteSpace(id) ? null : id);
        }
    }
}
