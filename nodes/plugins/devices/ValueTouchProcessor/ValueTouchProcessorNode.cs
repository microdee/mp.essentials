#region usings
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class TouchContainer
	{
		public Vector2D Point;
		public int ID;
		public Stopwatch Age = new Stopwatch();
		public int ExpireFrames = 0;
		public int AgeFrames = 0;
		
		public TouchContainer()
		{
			this.Age.Start();
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "TouchProcessor", Category = "Join")]
	#endregion PluginInfo
	public class JoinTouchProcessorNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Points")]
		public ISpread<Vector2D> FPoints;
		[Input("ID's")]
		public ISpread<int> FID;
		[Input("Keep for Frames", DefaultValue = 1)]
		public ISpread<int> FExpire;

		[Output("Container")]
		public ISpread<TouchContainer> FContainer;
		[Output("Point Out")]
		public ISpread<Vector2D> FPointsOut;
		[Output("ID")]
		public ISpread<int> FIDOut;
		[Output("Age")]
		public ISpread<double> FAge;
		[Output("Expiry")]
		public ISpread<int> FExpiry;
		[Output("New", IsBang = true)]
		public ISpread<bool> FNew;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		public Dictionary<int, TouchContainer> Touches = new Dictionary<int, TouchContainer>();
		public List<int> Removables = new List<int>();

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			SpreadMax = Math.Max(FPoints.SliceCount, FID.SliceCount);
			
			for(int i=0; i < SpreadMax; i++)
			{
				if(this.Touches.ContainsKey(FID[i]))
				{
					TouchContainer tc = this.Touches[FID[i]];
					tc.Point = FPoints[i];
					tc.ExpireFrames = 0;
				}
				else
				{
					TouchContainer tc = new TouchContainer();
					tc.Point = FPoints[i];
					tc.ID = FID[i];
					this.Touches.Add(FID[i], tc);
				}
			}
			this.Removables.Clear();
			foreach(KeyValuePair<int, TouchContainer> kvp in this.Touches)
			{
				if(kvp.Value.ExpireFrames > FExpire[0]) this.Removables.Add(kvp.Key);
			}
			for(int i=0; i<this.Removables.Count; i++)
			{
				this.Touches.Remove(this.Removables[i]);
			}
			
			FPointsOut.SliceCount = 0;
			FIDOut.SliceCount = 0;
			FAge.SliceCount = 0;
			FExpiry.SliceCount = 0;
			FNew.SliceCount = 0;
			FContainer.SliceCount = 0;
			int ii = 0;
			foreach(KeyValuePair<int, TouchContainer> kvp in this.Touches)
			{
				FContainer.Add(kvp.Value);
				FPointsOut.Add(kvp.Value.Point);
				FIDOut.Add(kvp.Value.ID);
				FAge.Add(kvp.Value.Age.Elapsed.TotalSeconds);
				FExpiry.Add(kvp.Value.ExpireFrames);
				FNew.Add(kvp.Value.AgeFrames == 0);
				
				kvp.Value.ExpireFrames++;
				kvp.Value.AgeFrames++;
				ii++;
			}
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "TouchProcessor", Category = "Split")]
	#endregion PluginInfo
	public class SplitTouchProcessorNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Container")]
		public Pin<TouchContainer> FInput;
		
		[Output("Point Out")]
		public ISpread<Vector2D> FPointsOut;
		[Output("ID")]
		public ISpread<int> FIDOut;
		[Output("Age")]
		public ISpread<double> FAge;
		[Output("Expiry")]
		public ISpread<int> FExpiry;
		[Output("New", IsBang = true)]
		public ISpread<bool> FNew;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		public Dictionary<int, TouchContainer> Touches = new Dictionary<int, TouchContainer>();
		public List<int> Removables = new List<int>();

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsConnected)
			{
				FPointsOut.SliceCount = FInput.SliceCount;
				FIDOut.SliceCount = FInput.SliceCount;
				FAge.SliceCount = FInput.SliceCount;
				FExpiry.SliceCount = FInput.SliceCount;
				FNew.SliceCount = FInput.SliceCount;
				
				for(int i=0; i<FInput.SliceCount; i++)
				{
					FPointsOut[i] = FInput[i].Point;
					FIDOut[i] = FInput[i].ID;
					FAge[i] = FInput[i].Age.Elapsed.TotalSeconds;
					FExpiry[i] = FInput[i].ExpireFrames-1;
					FNew[i] = FInput[i].AgeFrames == 1;
				}
			}
			else
			{
				FPointsOut.SliceCount = 0;
				FIDOut.SliceCount = 0;
				FAge.SliceCount = 0;
				FExpiry.SliceCount = 0;
				FNew.SliceCount = 0;
			}
		}
	}
}
