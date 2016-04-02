#region usings
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "SHA-256", Category = "String", Help = "Basic template with one string in/out", Tags = "")]
	#endregion PluginInfo
	public class StringSHA_256Node : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public ISpread<string> FInput;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		public string getHashSha256(string text)
	    {
	        byte[] bytes = Encoding.Unicode.GetBytes(text);
	        SHA256Managed hashstring = new SHA256Managed();
	        byte[] hash = hashstring.ComputeHash(bytes);
	        string hashString = string.Empty;
	        foreach (byte x in hash)
	        {
	            hashString += String.Format("{0:x2}", x);
	        }
	        return hashString;
	    }

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
				FOutput[i] = this.getHashSha256(FInput[i]);

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
