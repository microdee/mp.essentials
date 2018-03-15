using System;
using System.Net;
using mp.pddn;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(
        Name = "Client",
        Category = "Http",
        Help = "Construct a http client",
        Author = "microdee"
    )]
    #endregion PluginInfo
    public class HttpClientConstructor : ConstructorNode<HttpClientContainer>
    {
        [Input("Base Url", DefaultString = "http://localhost")]
        public ISpread<string> FUrl;

        [Input("Additional Default Header Name")]
        public IDiffSpread<ISpread<string>> FHeaderName;
        [Input("Additional Default Header Values")]
        public IDiffSpread<ISpread<string>> FHeaderValues;
        [Input("Additional Default Headers")]
        public ISpread<bool> FHeaders;
        [Input("Allow Untrusted")]
        public ISpread<bool> FTrustAll;

        [Input("Time Out", DefaultValue = 86400)]
        public ISpread<double> FTimeOut;
        [Output("Error")]
        public ISpread<string> FError;

        public override HttpClientContainer ConstructObject()
        {
            try
            {
                if (FTrustAll[CurrObj])
                {
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => true;
                }
                double milliseconds = FTimeOut[CurrObj] * 1000;
                HttpClientContainer hcc = new HttpClientContainer();
                hcc.Client.BaseAddress = new Uri(FUrl[CurrObj]);
                hcc.Client.Timeout = new TimeSpan(0, 0, 0, 0, (int)milliseconds);

                int cSpreadMax = 0;
                cSpreadMax = Math.Max(cSpreadMax, FHeaderName[CurrObj].SliceCount);
                cSpreadMax = Math.Max(cSpreadMax, FHeaderValues[CurrObj].SliceCount);

                if (FHeaders[CurrObj])
                {
                    for (int i = 0; i < cSpreadMax; i++)
                        hcc.Client.DefaultRequestHeaders.Add(FHeaderName[CurrObj][i], FHeaderValues[CurrObj][i]);
                }
                return hcc;
            }
            catch(Exception e)
            {
                FError[0] = e.Message + e.InnerException.Message;
                return null;
            }
        }
    }
}
