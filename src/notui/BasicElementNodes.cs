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
        Category = "Notui.Element",
        Version = "Join",
        Author = "microdee"
    )]
    public class RectangleElementNode : AbstractElementNode<RectangleElement> { }

    [PluginInfo(
        Name = "Circle",
        Category = "Notui.Element",
        Version = "Join",
        Author = "microdee"
    )]
    public class CircleElementNode : AbstractElementNode<CircleElement> { }
}
