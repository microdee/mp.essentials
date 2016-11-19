#region usings
using System;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using SevenZip.Compression.LZMA;

#endregion usings

namespace VVVV.Nodes
{
	public class CompressorIO
	{
		public CompressorIO() { }
		public byte[] InBytes;
		public byte[] OutBytes;
	}
	#region PluginInfo
	[PluginInfo(Name = "LZMA", Category = "Raw", Version = "Compress", Tags = "")]
	#endregion PluginInfo
	public class CompressRawLZMANode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<Stream> FStreamIn;
		
		[Input("Compress", IsBang = true)]
		public ISpread<bool> FCompress;
		[Input("Async")]
		public ISpread<bool> FAsync;

		[Output("Output")]
		public ISpread<Stream> FStreamOut;
		
		[Output("Working")]
		public ISpread<bool> FWorking;
		[Output("Error")]
		public ISpread<string> FError;
		
		private Spread<Task> Tasks = new Spread<Task>();
		private Spread<CompressorIO> IOs = new Spread<CompressorIO>();

		//when dealing with byte streams (what we call Raw in the GUI) it's always
		//good to have a byte buffer around. we'll use it when copying the data.
		//readonly byte[] FBuffer = new byte[1024];
		
		private void CompressAsync(object io)
		{
			CompressorIO IO = (CompressorIO)io;
			IO.OutBytes = SevenZipHelper.Compress(IO.InBytes);
		}
		#endregion fields & pins

		//called when all inputs and outputs defined above are assigned from the host
		public void OnImportsSatisfied()
		{
			//start with an empty stream output
			FStreamOut.SliceCount = 0;
		}
		

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			//ResizeAndDispose will adjust the spread length and thereby call
			//the given constructor function for new slices and Dispose on old
			//slices.
			FStreamOut.ResizeAndDispose(spreadMax, () => new MemoryStream());
			Tasks.SliceCount = spreadMax;
			IOs.SliceCount = spreadMax;
			FWorking.SliceCount = spreadMax;
			FError.SliceCount = spreadMax;
			for (int i = 0; i < spreadMax; i++)
			{
				if(!FAsync[i])
				{
					FWorking[i] = false;
				}
				if(FCompress[i])
				{
					IOs[i] = new CompressorIO();
					IOs[i].InBytes = new byte[FStreamIn[i].Length];
					FStreamIn[i].Position = 0;
					FStreamIn[i].Read(IOs[i].InBytes, 0, (int)FStreamIn[i].Length);
					if(FAsync[i])
					{
						Tasks[i] = new Task(CompressAsync, IOs[i]);
						Tasks[i].Start();
					}
					else
					{
						CompressAsync(IOs[i]);
						FWorking[i] = true;
						FStreamOut[i].Position = 0;
						FStreamOut[i].Write(IOs[i].OutBytes, 0, IOs[i].OutBytes.Length);
					}
				}
				if(Tasks[i] != null)
				{
					if(FAsync[i])
					{
						if((IOs[i].OutBytes != null) && FWorking[i])
						{
							FStreamOut[i].Position = 0;
							FStreamOut[i].Write(IOs[i].OutBytes, 0, IOs[i].OutBytes.Length);
						}
						FWorking[i] = (!Tasks[i].IsCompleted) && (!Tasks[i].IsFaulted);
					}
					if(Tasks[i].Exception != null)
						FError[i] = Tasks[i].Exception.Message;
				}
			}
			//this will force the changed flag of the output pin to be set
			FStreamOut.Flush(true);
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "LZMA", Category = "Raw", Version = "Decompress", Tags = "")]
	#endregion PluginInfo
	public class DecompressRawLZMANode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<Stream> FStreamIn;
		
		[Input("Decompress", IsBang = true)]
		public ISpread<bool> FDecompress;
		[Input("Async")]
		public ISpread<bool> FAsync;

		[Output("Output")]
		public ISpread<Stream> FStreamOut;
		
		[Output("Working")]
		public ISpread<bool> FWorking;
		[Output("Error")]
		public ISpread<string> FError;
		
		private Spread<Task> Tasks = new Spread<Task>();
		private Spread<CompressorIO> IOs = new Spread<CompressorIO>();

		//when dealing with byte streams (what we call Raw in the GUI) it's always
		//good to have a byte buffer around. we'll use it when copying the data.
		//readonly byte[] FBuffer = new byte[1024];
		
		private void DecompressAsync(object io)
		{
			CompressorIO IO = (CompressorIO)io;
			IO.OutBytes = SevenZipHelper.Decompress(IO.InBytes);
		}
		#endregion fields & pins

		//called when all inputs and outputs defined above are assigned from the host
		public void OnImportsSatisfied()
		{
			//start with an empty stream output
			FStreamOut.SliceCount = 0;
		}
		

		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
			//ResizeAndDispose will adjust the spread length and thereby call
			//the given constructor function for new slices and Dispose on old
			//slices.
			FStreamOut.ResizeAndDispose(spreadMax, () => new MemoryStream());
			Tasks.SliceCount = spreadMax;
			IOs.SliceCount = spreadMax;
			FWorking.SliceCount = spreadMax;
			FError.SliceCount = spreadMax;
			for (int i = 0; i < spreadMax; i++)
			{
				if(!FAsync[i])
				{
					FWorking[i] = false;
				}
				if(FDecompress[i])
				{
					IOs[i] = new CompressorIO();
					IOs[i].InBytes = new byte[FStreamIn[i].Length];
					FStreamIn[i].Position = 0;
					FStreamIn[i].Read(IOs[i].InBytes, 0, (int)FStreamIn[i].Length);
					if(FAsync[i])
					{
						Tasks[i] = new Task(DecompressAsync, IOs[i]);
						Tasks[i].Start();
					}
					else
					{
						DecompressAsync(IOs[i]);
						FWorking[i] = true;
						FStreamOut[i].Position = 0;
						FStreamOut[i].Write(IOs[i].OutBytes, 0, IOs[i].OutBytes.Length);
					}
				}
				if(Tasks[i] != null)
				{
					if(FAsync[i])
					{
						if((IOs[i].OutBytes != null) && FWorking[i])
						{
							FStreamOut[i].Position = 0;
							FStreamOut[i].Write(IOs[i].OutBytes, 0, IOs[i].OutBytes.Length);
						}
						FWorking[i] = (!Tasks[i].IsCompleted) && (!Tasks[i].IsFaulted);
					}
					if(Tasks[i].Exception != null)
						FError[i] = Tasks[i].Exception.Message;
				}
			}
			//this will force the changed flag of the output pin to be set
			FStreamOut.Flush(true);
		}
	}
}
