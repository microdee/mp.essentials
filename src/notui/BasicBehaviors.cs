using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction.Notui;
using md.stdl.Interaction.Notui.Behaviors;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.notui
{
    [PluginInfo(
        Name = "Sliding",
        Category = "Notui.Behavior",
        Version = "Join",
        Author = "microdee"
    )]
    public class SlidingBehaviorNode : AbstractBehaviorNode<SlidingBehavior> { }

    [PluginInfo(
        Name = "SlidingInfo",
        Category = "Notui.Behavior",
        Version = "Split",
        Author = "microdee"
    )]
    public class SlidingBehaviorInfoNode : BehaviorInstanceInfoNode<SlidingBehavior, SlidingBehavior.BehaviorState> { }

    [PluginInfo(
        Name = "ValueSlider2D",
        Category = "Notui.Behavior",
        Version = "Join",
        Author = "microdee"
    )]
    public class ValueSliderBehaviorNode : AbstractBehaviorNode<ValueSlider2D> { }
}
