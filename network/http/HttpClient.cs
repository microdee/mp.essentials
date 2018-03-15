using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using VVVV.Utils;

namespace VVVV.Nodes
{
    public enum SendMethod
    {
        Delete,
        Get,
        Head,
        Options,
        Post,
        Put,
        Trace
    }

    public class HttpRequestContainer
    {
        public Task<HttpResponseMessage> OngoingRequest { set; get; }
        public Task CopyTask;
    }
    public class HttpClientContainer : IDisposable
    {
        public HttpClient Client;
        public List<HttpRequestContainer> OngoingRequests = new List<HttpRequestContainer>();

        public HttpClientContainer()
        {
            Client = new HttpClient();
        }

        public HttpRequestContainer Send(HttpRequestMessage hrm, HttpCompletionOption hco)
        {
            var hrc = new HttpRequestContainer {OngoingRequest = Client.SendAsync(hrm, hco)};
            OngoingRequests.Add(hrc);
            return hrc;
        }

        public void Dispose()
        {
            OngoingRequests.Clear();
            Client.CancelPendingRequests();
            Client.Dispose();
        }
    }
}
