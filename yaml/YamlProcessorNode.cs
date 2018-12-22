using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using md.stdl.json;
using Newtonsoft.Json.Linq;
using VVVV.PluginInterfaces.V2;
using YamlDotNet.Serialization;

namespace mp.essentials.yaml
{
    [PluginInfo(
        Name = "YAML",
        Category = "YAML",
        Author = "microdee",
        Help = "Convert YAML content into vvvv readable formats"
    )]
    public class YamlProcessorNode : IPluginEvaluate
    {
        [Input("Input")]
        public IDiffSpread<string> Input;

        [Output("XML")]
        public ISpread<XElement> XmlOut;

        [Output("JSON")]
        public ISpread<string> JsonOut;

        [Output("JSON Object")]
        public ISpread<JObject> JsonObjOut;

        [Output("Error")]
        public ISpread<Exception> ErrorOut;

        public void Evaluate(int SpreadMax)
        {
            if (Input.IsChanged)
            {
                XmlOut.SliceCount = JsonObjOut.SliceCount = JsonOut.SliceCount = ErrorOut.SliceCount = Input.SliceCount;
                for (int i = 0; i < Input.SliceCount; i++)
                {
                    try
                    {
                        JsonObjOut[i] = YamlToJson.ParseYamlToJson(Input[i]);
                        JsonOut[i] = JsonObjOut[i].ToString();
                        XmlOut[i] = JsonObjOut[i].AsXElement();
                        ErrorOut[i] = null;
                    }
                    catch (Exception e)
                    {
                        ErrorOut[i] = e;
                    }
                }
            }
        }
    }
}
