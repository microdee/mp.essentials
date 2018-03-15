#region usings
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Network
{
	public class DownloadSlot
	{
	    public string URL;
	    public string Destination;
		public long Received = 0;
		public long Total = 0;
		public int Percent = 0;
	    public bool Ready = false;
	    public bool Success = false;
	    public bool Error = false;
	    public string Message = "";
		public WebClient Client;
		
		public DownloadSlot(string src, string dst)
		{
            Client = new WebClient();
            Client.DownloadProgressChanged += (sender, args) =>
            {
            	Received = args.BytesReceived;
            	Total = args.TotalBytesToReceive;
            	Percent = args.ProgressPercentage;
            };
		    Client.DownloadFileCompleted += (sender, args) =>
		    {
		        Ready = true;
		        if (args.Error != null)
		        {
		            Error = true;
		            Message = args.Error.Message;
		            Message += "\n" + args.Error.InnerException.Message;
		        }
		        else Success = true;
		    };
		    URL = src;
		    Destination = dst;
		}

	    public void Start()
	    {
            Client.DownloadFileAsync(new Uri(URL), Destination);
        }
	}

	[PluginInfo(
        Name = "Download",
        Category = "File",
        Author = "microdee"
        )]
	public class FileDownloadNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("URL", StringType = StringType.URL)]
		public ISpread<string> FURL;
		[Input("Destination", StringType = StringType.Filename, DefaultString = "foo.bar")]
		public ISpread<string> FDST;
		[Input("Start", IsBang = true)]
		public ISpread<bool> FStart;

        [Output("Download URL", StringType = StringType.URL)]
        public ISpread<string> FDURL;
        [Output("Destination", StringType = StringType.Filename)]
        public ISpread<string> FDDST;
        [Output("Received")]
		public ISpread<long> FRecieved;
		[Output("Total")]
		public ISpread<long> FTotal;
		[Output("Progress")]
		public ISpread<int> FProg;
		[Output("Ready", IsBang = true)]
		public ISpread<bool> FReady;
        [Output("Error", IsBang = true)]
        public ISpread<bool> FError;
        [Output("Error Message")]
        public ISpread<string> FMessage;

        protected Dictionary<string, DownloadSlot> slots = new Dictionary<string, DownloadSlot>();
		protected List<string> removeslot = new List<string>();

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            removeslot.Clear();
			for (int i = 0; i < FReady.SliceCount; i++)
			{
				if(FReady[i])
				{
				    FReady[i] = false;
                    removeslot.Add(FDURL[i]);
				}
			}
		    foreach (var k in removeslot)
		    {
		        slots.Remove(k);
		    }
		    for (int i = 0; i < SpreadMax; i++)
		    {
		        if(slots.ContainsKey(FURL[i])) continue;
                if(FURL[i] == "") continue;
		        if (FStart[i])
		        {
		            var slot = new DownloadSlot(FURL[i], FDST[i]);
		            slots.Add(FURL[i], slot);
		            slot.Start();
		        }
		    }
		    FDURL.SliceCount = slots.Count;
		    FDDST.SliceCount = slots.Count;
            FRecieved.SliceCount = slots.Count;
            FTotal.SliceCount = slots.Count;
            FProg.SliceCount = slots.Count;
            FReady.SliceCount = slots.Count;
            FError.SliceCount = slots.Count;
            FMessage.SliceCount = slots.Count;
            int ii = 0;
		    foreach (var s in slots.Values)
		    {
		        FDURL[ii] = s.URL;
		        FDDST[ii] = s.Destination;
		        FRecieved[ii] = s.Received;
                FTotal[ii] = s.Total;
                FProg[ii] = s.Percent;
		        FReady[ii] = s.Ready;
                FError[ii] = s.Error;
		        FMessage[ii] = s.Message;
                ii++;
		    }

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
