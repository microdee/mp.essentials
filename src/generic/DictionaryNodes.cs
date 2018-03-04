using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.Packs.Messaging;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.generic
{
    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "String",
        Author = "microdee"
    )]
    public class StringStringDictionaryNode : AbstractDictionaryNode<string, string> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "Int",
        Version = "String",
        Author = "microdee"
    )]
    public class IntStringDictionaryNode : AbstractDictionaryNode<int, string> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "Int",
        Author = "microdee"
    )]
    public class StringIntDictionaryNode : AbstractDictionaryNode<string, int> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "Int",
        Version = "Int",
        Author = "microdee"
    )]
    public class IntIntDictionaryNode : AbstractDictionaryNode<int, int> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "Value",
        Author = "microdee"
    )]
    public class StringValueDictionaryNode : AbstractDictionaryNode<string, double> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "2d",
        Author = "microdee"
    )]
    public class String2DDictionaryNode : AbstractDictionaryNode<string, Vector2D> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "3d",
        Author = "microdee"
    )]
    public class String3DDictionaryNode : AbstractDictionaryNode<string, Vector3D> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "4d",
        Author = "microdee"
    )]
    public class String4DDictionaryNode : AbstractDictionaryNode<string, Vector4D> { }
    
    [PluginInfo(
        Name = "Dictionary",
        Category = "String",
        Version = "Message",
        Author = "microdee"
    )]
    public class StringMessageDictionaryNode : AbstractDictionaryNode<string, Message> { }

    [PluginInfo(
        Name = "Dictionary",
        Category = "Message",
        Version = "Message",
        Author = "microdee"
    )]
    public class MessageMessageDictionaryNode : AbstractDictionaryNode<Message, Message> { }
}
