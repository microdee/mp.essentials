#region usings
using System;
using System.ComponentModel.Composition;
using SlimDX;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Quaternions
{
	[PluginInfo(
        Name = "Multiply",
        Category = "Quaternion",
        Version = "Spectral",
        Author = "microdee"
        )]
	public class SpectralQuaternionMultiplyNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public IDiffSpread<ISpread<Vector4D>> FInput;

		[Output("Output")]
		public ISpread<Vector4D> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            if(!FInput.IsChanged) return;
			FOutput.SliceCount = FInput.SliceCount;
		    if (FInput.SliceCount < 1)
		    {
		        FOutput.SliceCount = 1;
                FOutput[0] = new Vector4D(0, 0, 0, 1);
		    }

		    for (int i = 0; i < FInput.SliceCount; i++)
		    {

                if (FInput[i].SliceCount < 1)
                {
                    FOutput[i] = new Vector4D(0, 0, 0, 1);
                    continue;
                }
                if (FInput[i].SliceCount == 1)
		        {
		            FOutput[i] = FInput[i][0];
                    continue;
                }
                var sq = new Quaternion((float)FInput[i][0].x, (float)FInput[i][0].y, (float)FInput[i][0].z, (float)FInput[i][0].w);
                for (int j = 1; j < FInput[j].SliceCount; j++)
		        {
		            sq *= new Quaternion((float)FInput[i][j].x, (float)FInput[i][j].y, (float)FInput[i][j].z, (float)FInput[i][j].w);
		        }
                FOutput[i] = new Vector4D(sq.X, sq.Y, sq.Z, sq.W);
            }

			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
