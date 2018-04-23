#region usings
using System;
using System.ComponentModel.Composition;
using System.Linq;
using md.stdl.Boolean;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Boolean
{
	#region PluginInfo
	[PluginInfo(
        Name = "OR",
        Category = "Spectral",
        Version = "Bitwise",
        Author = "microdee"
        )]
	#endregion PluginInfo
	public class SpectralBitwiseValueORNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public ISpread<ISpread<uint>> FInput;

		[Output("Output")]
		public ISpread<uint> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;

			for (int i = 0; i < FOutput.SliceCount; i++)
			{
                uint flag = 0x0;
			    for (int j = 0; j < FInput[i].SliceCount; j++)
			    {
			        flag = flag | FInput[i][j];
			    }
				FOutput[i] = flag;
			}

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
    }

    #region PluginInfo
    [PluginInfo(
        Name = "OR",
        Category = "Value",
        Version = "Bitwise",
        Author = "microdee"
    )]
    #endregion PluginInfo
    public class BitwiseValueORNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", DefaultValue = 1.0, IsPinGroup = true)]
        public ISpread<ISpread<uint>> FInput;

        [Output("Output")]
        public ISpread<uint> FOutput;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                uint flag = 0x0;
                for (int j = 0; j < FInput.SliceCount; j++)
                {
                    flag = flag | FInput[j][i];
                }
                FOutput[i] = flag;
            }

            //FLogger.Log(LogType.Debug, "hi tty!");
        }
    }
}
