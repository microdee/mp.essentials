using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.PDDN;

namespace mp.essentials.shaders
{
    [ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions)]
    public class PreProcParamArgs
    {
        [ArgRequired]
        public string Type { get; set; }

        [ArgIgnore]
        public PreProcParamPinArgs PinAttr { get; private set; }

        [ArgActionMethod]
        public void Pin(PreProcParamPinArgs pinattr)
        {
            PinAttr = pinattr;
        }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions)]
    public class PreProcParamPinArgs
    {
        [ArgDefaultValue(PinVisibility.True)]
        public PinVisibility Visibility { get; set; }
    }

    public class PreprocessorOptionsNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Config("Shader Path Config")]
        public IDiffSpread<string> FShaderPathConf;

        protected override void Initialize()
        {
            base.Initialize();
        }

        public void Evaluate(int SpreadMax)
        {
            throw new NotImplementedException();
        }
    }
}
