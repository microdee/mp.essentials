#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using QRCode4CS;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "QRCode", Category = "Boolean", Help = "Basic template with one boolean in/out", Tags = "")]
	#endregion PluginInfo
	public class BooleanQRCodeNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<string> FInput;
		
		[Input("Error Correction", DefaultEnumEntry = "M")]
		public ISpread<QRErrorCorrectLevel> FErrorC;
		
		[Input("Type Number", DefaultValue = 4)]
		public ISpread<int> FTypeN;
		
		[Input("Generate", IsBang = true)]
		public ISpread<bool> FGen;

		[Output("Output")]
		public ISpread<ISpread<bool>> FOutput;
		
		[Output("Module Size")]
		public ISpread<int> FSize;
		
		[Output("Error")]
		public ISpread<string> FError;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;
			FSize.SliceCount = SpreadMax;
			FError.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if(FGen[i])
				{
					FError[i] = "";
					try
					{
						QRCode4CS.Options options = new QRCode4CS.Options(FInput[i]);
						options.TypeNumber = FTypeN[i];
						options.CorrectLevel = FErrorC[i];
						QRCode qrcode = new QRCode(options);
						qrcode.Make();
						int mcount = qrcode.GetModuleCount();
						FOutput[i].SliceCount = mcount*mcount;
						
						for (int row = 0; row < mcount; row++)
						{
							for (int col = 0; col < mcount; col++)
							{
								bool isDark = qrcode.IsDark(row, col);
								FOutput[i][row*mcount+col] = isDark;
							}
						}
						FSize[i] = mcount;
					}
					catch(Exception e)
					{
						FSize[i] = 0;
						FOutput[i].SliceCount = 0;
						FError[i] = e.Message;
					}
				}
			}

			//FLogger.Log(LogType.Debug, "hi TTY");
		}
	}
}
