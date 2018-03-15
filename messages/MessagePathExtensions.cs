using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VVVV.Packs.Messaging
{
    public class ObjectMessagePair
    {
        public object Object;
        public Message Source;
        public string Field;
        public int Index;
    }
    public static class MessagePath
    {
        public static string[] SplitIgnoringBetween(this string input, string separator, string ignorebetween)
        {
            return input.Split(ignorebetween.ToCharArray())
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new[] { ignorebetween + element + ignorebetween })  // Keep the entire item
                     .SelectMany(element => element).ToArray();
        }
        public static string[] MPathQueryKeys(this Message message)
        {
            return message.Fields.ToArray();
        }
        
        public static void MPath(this Message message, string path, List<ObjectMessagePair> Results, string Separator)
        {
            var levels = path.SplitIgnoringBetween(Separator, "`");

            //string[] levels = path.Split(Separator.ToCharArray());
            string nextpath = string.Join(Separator, levels, 1, levels.Length - 1);
            if ((levels[0][0] == '`') && (levels[0][levels[0].Length - 1] == '`'))
            {
                string key = levels[0].Trim('`');
                Regex Pattern = new Regex(key);
                foreach (string k in message.MPathQueryKeys())
                {
                    if (Pattern.Match(k).Value == string.Empty) continue;
                    if (levels.Length == 1)
                    {
                        int i = 0;
                        foreach (var o in message[k])
                        {
                            Results.Add(new ObjectMessagePair { Object = o, Source = message, Field = k, Index = i });
                            i++;
                        }
                    }
                    else message.MPathNextStep(k, nextpath, Results, Separator);
                }
            }
            else
            {
                if (levels.Length == 1)
                {
                    int i = 0;
                    foreach (var o in message[levels[0]])
                    {
                        Results.Add(new ObjectMessagePair { Object = o, Source = message, Field = levels[0], Index = i });
                        i++;
                    }
                    return;
                }
                message.MPathNextStep(levels[0], nextpath, Results, Separator);
            }
        }
        public static void MPathNextStep(this Message message, string CurrentLevel, string NextPath, List<ObjectMessagePair> Results, string Separator)
        {
            var bin = message[CurrentLevel];
            if (bin.GetInnerType() == typeof(Message))
            {
                foreach (var m in bin)
                {
                    var cm = (Message) m;
                    cm.MPath(NextPath, Results, Separator);
                }
            }
        }
        public static List<ObjectMessagePair> MPath(this Message message, string path, string Separator)
        {
            var Results = new List<ObjectMessagePair>();
            message.MPath(path, Results, Separator);
            return Results;
        }
    }
}
