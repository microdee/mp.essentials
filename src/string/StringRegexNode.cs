#region usings
using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Regex", Category = "String", Tags = "")]
	#endregion PluginInfo
	public class StringRegexNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public IDiffSpread<string> FInput;
		[Input("Pattern", DefaultString = @"(.*?)\sc\#")]
		public IDiffSpread<string> FPattern;
		[Input("Options", DefaultEnumEntry = "None")]
		public IDiffSpread<ISpread<RegexOptions>> FOptions;

		[Output("Matches")]
		public ISpread<ISpread<string>> FMatches;
		[Output("Start")]
		public ISpread<ISpread<int>> FStart;
		[Output("Length")]
		public ISpread<ISpread<int>> FLength;
		
		[Output("Captures")]
		public ISpread<ISpread<string>> FCaptures;
		[Output("Capture Start")]
		public ISpread<ISpread<int>> FCapStart;
		[Output("Capture Length")]
		public ISpread<ISpread<int>> FCapLength;
		[Output("Containing Group")]
		public ISpread<ISpread<int>> FGroup;
		
		public int fc = 0;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsChanged || FPattern.IsChanged || FOptions.IsChanged || fc==0)
			{
				FCaptures.SliceCount = FInput.SliceCount;
				FGroup.SliceCount = FInput.SliceCount;
				FStart.SliceCount = FInput.SliceCount;
				FLength.SliceCount = FInput.SliceCount;
				FMatches.SliceCount = FInput.SliceCount;
				FCapStart.SliceCount = FInput.SliceCount;
				FCapLength.SliceCount = FInput.SliceCount;
				
				for(int i=0; i<FInput.SliceCount; i++)
				{
					RegexOptions options = FOptions[i][0];
					for(int j=1; j<FOptions[i].SliceCount; i++)
					{
						options = options | FOptions[i][j];
					}
					Regex Pattern = new Regex(FPattern[i], options);
					MatchCollection mc = Pattern.Matches(FInput[i]);
					
					FCaptures[i].SliceCount = 0;
					FGroup[i].SliceCount = 0;
					FStart[i].SliceCount = 0;
					FLength[i].SliceCount = 0;
					FMatches[i].SliceCount = 0;
					FCapStart[i].SliceCount = 0;
					FCapLength[i].SliceCount = 0;
					
					foreach(Match m in mc)
					{
						FMatches[i].Add(m.Value);
						FStart[i].Add(m.Index);
						FLength[i].Add(m.Length);
						
						GroupCollection gc = m.Groups;
						int gid = 0;
						foreach(Group g in gc)
						{
							CaptureCollection cc = g.Captures;
							foreach(Capture c in cc)
							{
								if(gid != 0)
								{
									FCaptures[i].Add(c.Value);
									FCapStart[i].Add(c.Index);
									FCapLength[i].Add(c.Length);
									FGroup[i].Add(gid);
								}
							}
							gid++;
						}
					}
				}
			}
			fc++;
			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
