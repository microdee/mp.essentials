#region usings
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Sockets;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(
        Name = "UDPClient",
        Category = "Network",
        Version = "Async",
        Help = "Asynchronous UDP",
        Tags = "",
        AutoEvaluate = true
        )]
    #endregion PluginInfo
    public class UdpAsyncNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Data")]
        public ISpread<Stream> FData;
        [Input("Destination", StringType = StringType.IP)]
        public IDiffSpread<string> FDST;
        [Input("Local Port", DefaultValue = 44444)]
        public IDiffSpread<int> FLPort;
        [Input("Remote Port", DefaultValue = 4444)]
        public IDiffSpread<int> FRPort;
        [Input("Send", IsBang = true)]
        public ISpread<bool> FSend;

        protected UdpClient UDP;

        protected Dictionary<string, DownloadSlot> slots = new Dictionary<string, DownloadSlot>();
        protected List<string> removeslot = new List<string>();

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FDST.IsChanged || FLPort.IsChanged || FRPort.IsChanged)
            {
                UDP?.Dispose();
                UDP = new UdpClient(FLPort[0]);
                UDP.Connect(FDST[0], FRPort[0]);
            }
            if (FSend[0] && UDP != null)
            {
                if (FData.SliceCount != 0)
                {
                    var bbuf = new byte[FData[0].Length];
                    FData[0].Read(bbuf, 0, (int)FData[0].Length);
                    UDP.SendAsync(bbuf, (int) FData[0].Length);
                }
            }
        }
    }
}
