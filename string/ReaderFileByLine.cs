
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using System.IO;
using System.Text;

namespace mp.essentials.Nodes
{
    [PluginInfo(Name = "Reader", Category = "String", Version = "Line Wise", Help = "Returns a file line-wise", Tags = "file", Author = "woei, microdee")]
    public class ReaderFileByLine : IPluginEvaluate
    {
        [Input("Filename", StringType = StringType.Filename)]
        public ISpread<string> FilenameIn;
		
        [Input("Encoding", EnumName = "CodePages")]
        public IDiffSpread<EnumEntry> EncodingIn;
		
        [Input("Count", MinValue = 0, DefaultValue = 1)]
        public IDiffSpread<int> CountIn;
		
        [Input("Read", IsBang = true)]
        public ISpread<bool> ReadIn;
		
        [Input("Restart Stream", IsBang = true)]
        public ISpread<bool> RestartStreamIn;
		
        [Output("Content")]
        public ISpread<string> ContentOut;
		
        [Output("Stream Position")]
        public ISpread<long> PositionOut;
		
        [Output("Stream Size")]
        public ISpread<long> SizeOut;
		
        [Output("Progress")]
        public ISpread<double> ProgressOut;
		
        [Output("End Of Stream")]
        public ISpread<bool> EosOut;
		
        [Output("Error")]
        public ISpread<string> ErrorOut;

        readonly Spread<string> PathCache = new Spread<string>(0);
        readonly Spread<StreamReader> StreamReaderCache = new Spread<StreamReader>(0);
        
        public void Evaluate(int spreadMax)
        {
			ContentOut.SliceCount = spreadMax;
			EosOut.SliceCount = spreadMax;
			PositionOut.SliceCount = spreadMax;
			SizeOut.SliceCount = spreadMax;
			ProgressOut.SliceCount = spreadMax;
			
			PathCache.ResizeAndDismiss(spreadMax, () => "");
			StreamReaderCache.Resize(
				spreadMax,
				(i) => File.Exists(FilenameIn[i]) ? new StreamReader(FilenameIn[i]) : null,
				(t) => t.Dispose()
			);
			
			for (int i = 0; i < spreadMax; i++)
			{
				//check if encoding has changed
				var enc = EncodingIn[i].Index > 0 ? Encoding.GetEncoding(EncodingIn[i].Name) : Encoding.Default;
				
				//initialize stream reader
				bool update = false;
				bool isValid = true;
				if (RestartStreamIn[i] || (ReadIn[i] && StreamReaderCache[i] == null))
				{
					try
					{
						StreamReaderCache[i]?.Dispose();
						StreamReaderCache[i] = new StreamReader(FilenameIn[i], enc);
						PathCache[i] = FilenameIn[i];
						ErrorOut[i] = "";
					}
					catch (Exception e)
					{
						ErrorOut[i] = e.Message + Environment.NewLine + e.StackTrace;
						EosOut[i] = true;
					}
					ContentOut[i] = "";
				}
				
				PositionOut[i] = StreamReaderCache[i]?.BaseStream.Position ?? 0; 
				SizeOut[i] = StreamReaderCache[i]?.BaseStream.Length ?? 0;
				ProgressOut[i] = StreamReaderCache[i] == null ? 1.0 : (double) PositionOut[i] / (double) SizeOut[i];
				EosOut[i] = StreamReaderCache[i]?.EndOfStream ?? true;

				if (ReadIn[i] && !EosOut[i])
				{
					ContentOut[i] = "";
					for (int currLine = 0; currLine < CountIn[i]; currLine++)
					{
						ContentOut[i] += StreamReaderCache[i].ReadLine() + Environment.NewLine;
					}
					ContentOut[i] = ContentOut[i].TrimEnd();
				}
			}
        }
    }
}