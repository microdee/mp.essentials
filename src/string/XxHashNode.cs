#region usings
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;
using Extensions.Data;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Strings
{
    [PluginInfo(
        Name = "XxHash32",
        Category = "String",
        Author = "microdee"
    )]
    public class StringXxHash32Node : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", DefaultString = "hello c#")]
        public IDiffSpread<string> FInput;

        [Output("Output")]
        public ISpread<uint> FOutput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins


        private readonly XXHash.State32 _xxHashState = XXHash.CreateState32(51423);

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if(!FInput.IsChanged) return;

            FOutput.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                var inbytes = Encoding.Default.GetBytes(FInput[i]);
                XXHash.ResetState32(_xxHashState, 51423);
                XXHash.UpdateState32(_xxHashState, inbytes, 0, inbytes.Length);
                FOutput[i] = XXHash.DigestState32(_xxHashState);
            }
        }
    }
}
