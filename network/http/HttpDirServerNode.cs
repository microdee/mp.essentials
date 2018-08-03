using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.network.http
{
    [PluginInfo(
        Name = "HttpDirServer",
        Category = "Network",
        Help = "Serve static content from any folder over http through specified or auto-selected port",
        Author = "microdee"
    )]
    public class HttpDirServerNode : IPluginEvaluate
    {
        [Input("Directory", StringType = StringType.Directory)]
        public ISpread<string> FDir;
        [Input("Port")]
        public ISpread<int> FPort;
        [Input("Start", IsBang = true)]
        public ISpread<bool> FStart;

        [Output("Port Out")]
        public ISpread<int> FPortOut;
        [Output("Url")]
        public ISpread<string> FurlOut;

        private SimpleHTTPServer _httpServer;

        public void Evaluate(int SpreadMax)
        {
            if (FStart[0])
            {
                _httpServer?.Stop();
                if(FPort[0] <= 0) _httpServer = new SimpleHTTPServer(FDir[0]);
                else _httpServer = new SimpleHTTPServer(FDir[0], FPort[0]);
                FPortOut[0] = _httpServer.Port;
                FurlOut[0] = $"http://localhost:{_httpServer.Port}/";
            }
        }
    }
}
