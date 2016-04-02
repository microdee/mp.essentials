#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "GetQuaternion", Category = "Transform", Help = "Basic template with one transform in/out", Tags = "matrix")]
	#endregion PluginInfo
	public class TransformGetQuaternionNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<Matrix4x4> FInput;

		[Output("Output")]
		public ISpread<Vector4D> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				Matrix4x4 im = FInput[i].Transpose();
				double tr = im.m11 + im.m22 + im.m33;
				double qw, qx, qy, qz = 0;
				if (tr > 0) { 
					double S = Math.Sqrt(tr+1.0) * 2; // S=4*qw 
					qw = 0.25 * S;
					qx = (im.m32 - im.m23) / S;
					qy = (im.m13 - im.m31) / S; 
					qz = (im.m21 - im.m12) / S; 
				} else if ((im.m11 > im.m22)&(im.m11 > im.m33)) { 
					double S = Math.Sqrt(1.0 + im.m11 - im.m22 - im.m33) * 2; // S=4*qx 
					qw = (im.m32 - im.m23) / S;
					qx = 0.25 * S;
					qy = (im.m12 + im.m21) / S; 
					qz = (im.m13 + im.m31) / S; 
				} else if (im.m22 > im.m33) { 
					double S = Math.Sqrt(1.0 + im.m22 - im.m11 - im.m33) * 2; // S=4*qy
					qw = (im.m13 - im.m31) / S;
					qx = (im.m12 + im.m21) / S; 
					qy = 0.25 * S;
					qz = (im.m23 + im.m32) / S; 
				} else { 
					double S = Math.Sqrt(1.0 + im.m33 - im.m11 - im.m22) * 2; // S=4*qz
					qw = (im.m21 - im.m12) / S;
					qx = (im.m13 + im.m31) / S;
					qy = (im.m23 + im.m32) / S;
					qz = 0.25 * S;
				}
				FOutput[i] = new Vector4D(qx, qy, qz, qw);
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
