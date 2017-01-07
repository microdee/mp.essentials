using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.VObjects.Http.HttpNodes
{
    [PluginInfo(
        Name = "Send",
        Category = "Http",
        Help = "Send an HTTP message",
        AutoEvaluate = true,
        Author = "microdee"
    )]
    public class HttpClientSendNode : IPluginEvaluate
    {
        [Input("HTTP Client")]
        public Pin<HttpClientContainer> FClient;

        [Input("Path")]
        public ISpread<ISpread<string>> FPath;
        [Input("Content")]
        public ISpread<ISpread<string>> FContentIn;
        [Input("Media Type")]
        public ISpread<ISpread<string>> FMediaType;
        [Input("Method", DefaultEnumEntry = "Get")]
        public ISpread<ISpread<SendMethod>> FMethod;

        [Input("Completion On", DefaultEnumEntry = "ResponseContentRead")]
        public ISpread<HttpCompletionOption> FCompletion;
        //[Input("Flush Every Frame")]
        //public ISpread<bool> FFlush;
        [Input("Send", IsBang = true)]
        public ISpread<ISpread<bool>> FSend;
        [Input("Long Polling")]
        public ISpread<bool> FLongPoll;

        [Output("Request")]
        public ISpread<ISpread<HttpRequestContainer>> FRequest;
        [Output("Headers")]
        public ISpread<ISpread<HttpResponseHeaders>> FHeaders;
        [Output("Content")]
        public ISpread<ISpread<Stream>> FContent;
        [Output("Content Size")]
        public ISpread<ISpread<int>> FSize;
        [Output("Status Code")]
        public ISpread<ISpread<string>> FStatus;
        [Output("Reason")]
        public ISpread<ISpread<string>> FReason;
        [Output("Sending")]
        public ISpread<ISpread<bool>> FSending;
        //[Output("Data Received", IsBang = true)]
        //public ISpread<ISpread<bool>> FReceived;
        [Output("Completed", IsBang = true)]
        public ISpread<ISpread<bool>> FCompleted;

        protected Dictionary<Tuple<int, int>, Stream> Buffers = new Dictionary<Tuple<int, int>, Stream>();

        protected void Send(int i, int j, HttpClientContainer hcc)
        {
            FSending[i][j] = true;
            HttpMethod hm = new HttpMethod(FMethod[i][j].ToString());
            HttpRequestMessage hrm = new HttpRequestMessage(hm, FPath[i][j]);
            if (FMethod[i][j] != SendMethod.Get)
            {
                StringContent sc = new StringContent(FContentIn[i][j], Encoding.UTF8, FMediaType[i][j]);
                hrm.Content = sc;
            }
            var request = hcc.Send(hrm, FCompletion[i]);
            FRequest[i][j] = request;
        }

        public void Evaluate(int SpreadMax)
        {
            if (!FClient.IsConnected) return;

            FRequest.SliceCount = FClient.SliceCount;
            FHeaders.SliceCount = FClient.SliceCount;
            FContent.SliceCount = FClient.SliceCount;
            FStatus.SliceCount = FClient.SliceCount;
            FReason.SliceCount = FClient.SliceCount;
            FSending.SliceCount = FClient.SliceCount;
            FCompleted.SliceCount = FClient.SliceCount;
            //FReceived.SliceCount = FClient.SliceCount;
            FSize.SliceCount = FClient.SliceCount;
            for (int i = 0; i < FClient.SliceCount; i++)
            {
                int cSpreadMax = 0;
                cSpreadMax = Math.Max(cSpreadMax, FPath[i].SliceCount);
                cSpreadMax = Math.Max(cSpreadMax, FContentIn[i].SliceCount);
                cSpreadMax = Math.Max(cSpreadMax, FMethod[i].SliceCount);
                cSpreadMax = Math.Max(cSpreadMax, FMediaType[i].SliceCount);

                FRequest[i].SliceCount = cSpreadMax;
                FHeaders[i].SliceCount = cSpreadMax;
                FContent[i].SliceCount = cSpreadMax;
                FStatus[i].SliceCount = cSpreadMax;
                FReason[i].SliceCount = cSpreadMax;
                FSending[i].SliceCount = cSpreadMax;
                FCompleted[i].SliceCount = cSpreadMax;
                //FReceived[i].SliceCount = cSpreadMax;
                FSize[i].SliceCount = cSpreadMax;

                var hcc = FClient[i];
                for (int j = 0; j < cSpreadMax; j++)
                {
                    if (FSend[i][j])
                    {
                        Send(i, i, hcc);
                    }
                    
                    if (FRequest[i][j] == null) continue;
                    if (!FRequest[i][j].OngoingRequest.IsCompleted) continue;

                    //var bufaddr = new Tuple<int, int>(i, j);
                    if (FContent[i][j] == null) FContent[i][j] = new MemoryStream();
                    //if (!Buffers.ContainsKey(bufaddr))
                    //    Buffers.Add(bufaddr, new MemoryStream());

                    //var prevpos = Buffers[bufaddr].Position;
                    //FContent[i][j].Position = FContent[i][j].Length;
                    //Buffers[bufaddr].Position = FContent[i][j].Length;
                    //Buffers[bufaddr].CopyTo(FContent[i][j]);
                    //Buffers[bufaddr].Position = prevpos;
                    
                    FSize[i][j] = (int)FContent[i][j].Position;

                    FContent.Flush(true);
                    FContent[i].Stream.IsChanged = true;
                    FContent.Stream.IsChanged = true;
                    FCompleted[i][j] = false;

                    var result = FRequest[i][j].OngoingRequest.Result;

                    if (FRequest[i][j].CopyTask == null || (FRequest[i][j].CopyTask?.IsCompleted ?? true))
                        FRequest[i][j].CopyTask = result.Content.CopyToAsync(FContent[i][j]);
                    
                    if (FSending[i][j]) FCompleted[i][j] = true;
                    FHeaders[i][j] = result.Headers;
                    FStatus[i][j] = result.StatusCode.ToString();
                    FReason[i][j] = result.ReasonPhrase;
                    FSending[i][j] = false;

                    if (FLongPoll[i] && FRequest[i][j].CopyTask.IsCompleted)
                    {
                        Send(i, i, hcc);
                    }
                }
            }
        }
    }
}
