using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.windows
{
    [PluginInfo(
        Name = "PostMessage",
        Category = "Windows",
        Tags = "spreadable, SendMessage",
        Help = "Same as SendMessage but spreadable and async",
        Author = "microdee",
        AutoEvaluate = true
    )]
    public class WindowsPostMessageNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Handle", DefaultValue = 0)]
        public ISpread<long> FHandle;
        [Input("Message")]
        public ISpread<uint> FMessage;
        [Input("WParam")]
        public ISpread<uint> FWp;
        [Input("LParam")]
        public ISpread<long> FLp;
        [Input("Send", IsBang = true)]
        public ISpread<bool> FSend;

        [Output("Result")]
        public ISpread<bool> FResult;
        #endregion fields & pins

        [DllImport("C:\\Windows\\System32\\user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, long lParam);

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FResult.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                if (FSend[i])
                {
                    FResult[i] = PostMessage(new IntPtr(FHandle[i]), FMessage[i], FWp[i], FLp[i]);
                }
            }
        }
    }
}
