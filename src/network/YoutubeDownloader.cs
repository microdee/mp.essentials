#region usings
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using YoutubeExtractor;

#endregion usings

namespace mp.essentials.Nodes.Network
{
    public enum YoutubeVideoQuality
    {
        p140 = 140,
        p240 = 240,
        p360 = 360,
        p480 = 480,
        p720 = 720,
        p1080 = 1080,
        p1440 = 1440,
        p2160 = 2160
    }
    public enum YoutubeVideoType
    {
        Mp4 = VideoType.Mp4,
        WebM = VideoType.WebM
    }
    public class YoutubeDownloadSlot
	{
	    public string URL;
	    public string Destination;
	    public IEnumerable<VideoInfo> AllVideoInfo;
	    public VideoInfo CurrentVideoInfo;
		public double Progress = 0;
	    public bool Ready = false;
	    public bool Success = false;
	    public bool Error = false;
	    public string Message = "";
		public VideoDownloader VideoClient;
	    public AudioDownloader AudioClient;
	    public Task DownloadTask;

        public YoutubeDownloadSlot(string src, string dst, bool audio, int resolution, YoutubeVideoType type)
        {
            URL = src;
		    try
		    {
		        AllVideoInfo = DownloadUrlResolver.GetDownloadUrls(src);
		        if (AllVideoInfo == null) throw new Exception("Problem with VideoInfo list.");
		        if (audio)
		        {
		            try
		            {
		                CurrentVideoInfo = AllVideoInfo
		                    .Where(vi => vi.CanExtractAudio)
		                    .OrderByDescending(va => va.AudioBitrate)
		                    .First();

		                if (CurrentVideoInfo.RequiresDecryption) DownloadUrlResolver.DecryptDownloadUrl(CurrentVideoInfo);

		                var filename = Regex.Replace(CurrentVideoInfo.Title, @"[^a-zA-Z0-9-\.]", "");
		                Destination = Path.Combine(dst, filename + CurrentVideoInfo.AudioExtension);
		                AudioClient = new AudioDownloader(CurrentVideoInfo, Destination);
		                AudioClient.DownloadProgressChanged += (sender, args) => Progress = args.ProgressPercentage / 100;
		                AudioClient.AudioExtractionProgressChanged += (sender, args) => Progress = args.ProgressPercentage / 100 + 1;
		                AudioClient.DownloadFinished += (sender, args) =>
		                {
		                    Ready = true;
		                    Success = true;
		                };
                    }
		            catch (Exception e)
		            {
		                Message = "Audio only is not available. Falling back to video";
		                audio = false;
                    }
		        }
		        if (!audio)
		        {
		            var filteredinfo = AllVideoInfo
		                .Where(vi => vi.VideoType == (VideoType)type && vi.AudioType != AudioType.Unknown);
		            CurrentVideoInfo = filteredinfo.First(vi =>
		            {
		                if (filteredinfo.Any(vii => vii.Resolution == resolution))
		                    return vi.Resolution == resolution;
		                else return vi.Resolution == filteredinfo.Max(vii => vii.Resolution);
		            });

                    if (CurrentVideoInfo.RequiresDecryption) DownloadUrlResolver.DecryptDownloadUrl(CurrentVideoInfo);

		            var filename = Regex.Replace(CurrentVideoInfo.Title, @"[^a-zA-Z0-9-\.]", "");
                    Destination = Path.Combine(dst, filename + CurrentVideoInfo.VideoExtension);
                    VideoClient = new VideoDownloader(CurrentVideoInfo, Destination);
		            VideoClient.DownloadProgressChanged += (sender, args) => Progress = args.ProgressPercentage / 100;
		            VideoClient.DownloadFinished += (sender, args) =>
		            {
		                Ready = true;
		                Success = true;
		            };
                }
		    }
		    catch (Exception e)
		    {
		        Ready = true;
		        Error = true;
		        Message = e.Message;
		    }
		}

	    public void Start()
	    {
	        if (VideoClient == null)
	        {
	            DownloadTask = Task.Factory.StartNew(() =>
	            {
	                try
	                {
	                    AudioClient.Execute();
	                }
	                catch (Exception e)
	                {
	                    Ready = true;
                        Error = true;
	                    Message = e.Message;
	                }
	            });
            }
	        if (AudioClient == null)
	        {
	            DownloadTask = Task.Factory.StartNew(() =>
	            {
	                try
	                {
	                    VideoClient.Execute();
	                }
	                catch (Exception e)
	                {
	                    Ready = true;
                        Error = true;
	                    Message = e.Message;
	                }
	            });
            }
	    }
	}

	[PluginInfo(
        Name = "YoutubeDownloader",
        Category = "File",
        Author = "microdee"
        )]
	public class YoutubeDownloaderNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Youtube Link", StringType = StringType.URL)]
		public ISpread<string> FURL;
		[Input("Destination Folder", StringType = StringType.Directory, DefaultString = "")]
		public ISpread<string> FDSTDir;
	    [Input("Resolution", DefaultEnumEntry = "p1080")]
	    public ISpread<YoutubeVideoQuality> FResolution;
	    [Input("Type", DefaultEnumEntry = "Mp4")]
	    public ISpread<YoutubeVideoType> FYTType;
        [Input("Try Audio Only")]
	    public ISpread<bool> FAudioOnly;
        [Input("Start", IsBang = true)]
		public ISpread<bool> FStart;

	    [Output("Youtube Link Out", StringType = StringType.URL)]
	    public ISpread<string> FDURL;
        [Output("Video Info", StringType = StringType.URL)]
        public ISpread<VideoInfo> FVideoInfo;
        [Output("Destination", StringType = StringType.Filename)]
        public ISpread<string> FDDST;
		[Output("Progress")]
		public ISpread<double> FProg;
		[Output("Ready", IsBang = true)]
		public ISpread<bool> FReady;
        [Output("Error", IsBang = true)]
        public ISpread<bool> FError;
        [Output("Error Message")]
        public ISpread<string> FMessage;

        protected Dictionary<string, YoutubeDownloadSlot> slots = new Dictionary<string, YoutubeDownloadSlot>();
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
		            var slot = new YoutubeDownloadSlot(FURL[i], FDSTDir[i], FAudioOnly[i], (int)FResolution[i], FYTType[i]);
		            slots.Add(FURL[i], slot);
		            slot.Start();
		        }
		    }
		    FDURL.SliceCount = slots.Count;
		    FVideoInfo.SliceCount = slots.Count;
		    FDDST.SliceCount = slots.Count;
            FProg.SliceCount = slots.Count;
            FReady.SliceCount = slots.Count;
            FError.SliceCount = slots.Count;
            FMessage.SliceCount = slots.Count;
            int ii = 0;
		    foreach (var s in slots.Values)
		    {
		        FDURL[ii] = s.URL;
		        FVideoInfo[ii] = s.CurrentVideoInfo;
		        FDDST[ii] = s.Destination;
                FProg[ii] = s.Progress;
		        FReady[ii] = s.Ready;
                FError[ii] = s.Error;
		        FMessage[ii] = s.Message;
                ii++;
		    }

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
