#region usings
using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.Animation;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class TouchContainer
	{
		private Vector2D PrevPoint;
		private Vector2D PrevPointF;
		
		public Vector2D Point;
		public Vector2D PointFiltered;
		public Vector2D Velocity;
		public Vector2D VelocityFiltered;
		public int ID;
		public Stopwatch Age = new Stopwatch();
		public int ExpireFrames = 0;
		public int AgeFrames = 0;
		public bool UseFiltered = false;
		
		private OneEuroFilter FilterX;
		private OneEuroFilter FilterY;
		
		public double FilterMinCutoff
		{
			get { return FilterX.MinCutoff; }
			set
			{
				FilterX.MinCutoff = value;
				FilterY.MinCutoff = value;
			}
		}
		public double FilterBeta
		{
			get { return FilterX.Beta; }
			set
			{
				FilterX.Beta = value;
				FilterY.Beta = value;
			}
		}
		public double FilterCutoffDerivative
		{
			get { return FilterX.CutoffDerivative; }
			set
			{
				FilterX.CutoffDerivative = value;
				FilterY.CutoffDerivative = value;
			}
		}
		
		public TouchContainer(double fmincutoff, double fbeta)
		{
			Age.Start();
			FilterX = new OneEuroFilter(fmincutoff, fbeta);
			FilterY = new OneEuroFilter(fmincutoff, fbeta);
		}
		public void Update(double deltaft)
		{
			var rate = 1 / deltaft;
			
			PointFiltered = new Vector2D(
				FilterX.Filter(Point.x, rate),
				FilterY.Filter(Point.y, rate)
			);
			
			if(AgeFrames <= 1)
			{
				PrevPoint = new Vector2D(Point.x, Point.y);
				PrevPointF = new Vector2D(PointFiltered.x, PointFiltered.y);
				
				Velocity = new Vector2D(0,0);
				VelocityFiltered = new Vector2D(0,0);
			}
			else
			{
				Velocity = new Vector2D(
					Point.x - PrevPoint.x,
					Point.y - PrevPoint.y
				);
				PrevPoint = new Vector2D(Point.x, Point.y);
				
				VelocityFiltered = new Vector2D(
					PointFiltered.x - PrevPointF.x,
					PointFiltered.y - PrevPointF.y
				);
				PrevPointF = new Vector2D(PointFiltered.x, PointFiltered.y);
			}
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "TouchProcessor", Category = "Join", AutoEvaluate = true)]
	#endregion PluginInfo
	public class JoinTouchProcessorNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Points")]
		public ISpread<Vector2D> FPoints;
		[Input("ID's")]
		public ISpread<int> FID;
        [Input("Auto ID")]
        public ISpread<bool> FAutoID;
        [Input("Auto ID Distance Threshold", DefaultValue = 0.1)]
        public ISpread<double> FAIDDistThr;
        [Input("Keep for Frames", DefaultValue = 1)]
        public ISpread<int> FExpire;

        [Input("Use 1€ Filter")]
		public ISpread<bool> FUseFilter;
		[Input("Minimum Cutoff", DefaultValue = 1.0, MinValue = 0.0, MaxValue = 10.0)]
		public ISpread<double> FMinCutoff;
		[Input("Beta", DefaultValue = 0.7, MinValue = 0.0, MaxValue = 10.0)]
		public ISpread<double> FBeta;
		[Input("Cutoff for Derivative", DefaultValue = 1.0, MinValue = 0.0, MaxValue = 10.0, Visibility = PinVisibility.OnlyInspector)]
		public ISpread<double> FCutoffDerivative;

		[Output("Container")]
		public ISpread<TouchContainer> FContainer;
		
		[Output("Point Out")]
		public ISpread<Vector2D> FPointsOut;
		[Output("Velocity Out")]
		public ISpread<Vector2D> FVelOut;
		
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
		[Import()]
		public IHDEHost FHDEHost;
        #endregion fields & pins

        private Spread<Vector2D> PrevTouchSpread = new Spread<Vector2D>();
        private Spread<int> AutoIDs = new Spread<int>();
        private Spread<int> StagingAutoIDs = new Spread<int>();
        private Spread<bool> TagPoint = new Spread<bool>();
        private bool Init = true;
		
		public Dictionary<int, TouchContainer> Touches = new Dictionary<int, TouchContainer>();
		public List<int> Removables = new List<int>();
		
		public double lastftime = 0;
		
        public void OnImportsSatisfied()
        {
            lastftime = FHDEHost.FrameTime;
        }


		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            if(Init)
            {
                PrevTouchSpread.SliceCount = 0;
                AutoIDs.SliceCount = 0;
                Init = false;
            }
            if(FAutoID[0])
            {
                if (FPoints.SliceCount > PrevTouchSpread.SliceCount)
                {
                    AutoIDs.SliceCount = FPoints.SliceCount;
                    StagingAutoIDs.SliceCount = FPoints.SliceCount;
                    TagPoint.SliceCount = FPoints.SliceCount;
                    for (int i = 0; i < TagPoint.SliceCount; i++)
                    {
                        TagPoint[i] = false;
                    }
                    for (int i=0; i<PrevTouchSpread.SliceCount; i++)
                    {
                        int paid = AutoIDs[i];
                        double dist = double.MaxValue;
                        int cid = 0;
                        for (int j = 0; j < FPoints.SliceCount; j++)
                        {
                            double tdist = VMath.Dist(FPoints[j], PrevTouchSpread[i]);
                            if((tdist < dist) && (!TagPoint[j]) && (tdist < FAIDDistThr[0]))
                            {
                                cid = j;
                                dist = tdist;
                            }
                        }
                        StagingAutoIDs[cid] = paid;
                        TagPoint[cid] = true;
                    }
                    for (int i = 0; i < TagPoint.SliceCount; i++)
                    {
                        AutoIDs[i] = StagingAutoIDs[i];
                        if(!TagPoint[i]) // NEW
                        {
                            AutoIDs[i] = new Random().Next();
                        }
                    }
                }
                if (FPoints.SliceCount < PrevTouchSpread.SliceCount)
                {
                    for (int i = 0; i < TagPoint.SliceCount; i++)
                    {
                        TagPoint[i] = false;
                    }
                    for (int i = 0; i < FPoints.SliceCount; i++)
                    {
                        double dist = double.MaxValue;
                        int cid = 0;
                        for (int j = 0; j < PrevTouchSpread.SliceCount; j++)
                        {
                            double tdist = VMath.Dist(FPoints[i], PrevTouchSpread[j]);
                            if ((tdist < dist) && (!TagPoint[j]) && (tdist < FAIDDistThr[0]))
                            {
                                cid = j;
                                dist = tdist;
                            }
                        }
                        TagPoint[cid] = true;
                    }
                    /*
                    if (AutoIDs.SliceCount != 0)
                    {
                        for (int i = 0; i < TagPoint.SliceCount; i++)
                        {
                            if (!TagPoint[i]) // OLD
                            {
                                AutoIDs.RemoveAt(i);
                            }
                        }
                    }
                    */
                    AutoIDs.AssignFrom(AutoIDs.Where((int i, int j) => { return TagPoint[j]; }));
                }
                PrevTouchSpread.SliceCount = FPoints.SliceCount;
                for(int i=0; i<PrevTouchSpread.SliceCount; i++)
                {
                    PrevTouchSpread[i] = new Vector2D(FPoints[i]);
                }
            }

			SpreadMax = Math.Min(FPoints.SliceCount, FID.SliceCount);
			
			var dt = FHDEHost.FrameTime - lastftime;
			lastftime = FHDEHost.FrameTime;
			
			for(int i=0; i < SpreadMax; i++)
			{
                int tid = FID[i];
                if (FAutoID[0]) tid = AutoIDs[i];
				if(this.Touches.ContainsKey(tid))
				{
					TouchContainer tc = Touches[tid];
					tc.FilterMinCutoff = FMinCutoff[i];
					tc.FilterBeta = FBeta[i];
					tc.FilterCutoffDerivative = FCutoffDerivative[i];
					tc.Point = FPoints[i];
					tc.ExpireFrames = 0;
					tc.Update(dt);
				}
				else
				{
					TouchContainer tc = new TouchContainer(FMinCutoff[i], FBeta[i]);
					tc.UseFiltered = FUseFilter[i];
					tc.FilterCutoffDerivative = FCutoffDerivative[i];
					tc.Point = FPoints[i];
					tc.PointFiltered = FPoints[i];
					tc.ID = tid;
					this.Touches.Add(tid, tc);
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
			FVelOut.SliceCount = 0;
			FIDOut.SliceCount = 0;
			FAge.SliceCount = 0;
			FExpiry.SliceCount = 0;
			FNew.SliceCount = 0;
			FContainer.SliceCount = 0;
			int ii = 0;
			
			foreach(KeyValuePair<int, TouchContainer> kvp in this.Touches)
			{
				FContainer.Add(kvp.Value);
				if(kvp.Value.UseFiltered)
				{
					FPointsOut.Add(kvp.Value.PointFiltered);
					FVelOut.Add(kvp.Value.VelocityFiltered);
				}
				else
				{
					FPointsOut.Add(kvp.Value.Point);
					FVelOut.Add(kvp.Value.Velocity);
				}
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
		public Pin<object> FInput;
		
		[Output("Point Out")]
		public ISpread<Vector2D> FPointsOut;
		[Output("Velocity Out")]
		public ISpread<Vector2D> FVelOut;
		
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

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInput.IsConnected && (FInput.SliceCount != 0))
			{
				if(FInput[0] is TouchContainer)
				{
					FPointsOut.SliceCount = FInput.SliceCount;
					FVelOut.SliceCount = FInput.SliceCount;
					FIDOut.SliceCount = FInput.SliceCount;
					FAge.SliceCount = FInput.SliceCount;
					FExpiry.SliceCount = FInput.SliceCount;
					FNew.SliceCount = FInput.SliceCount;
					
					for(int i=0; i<FInput.SliceCount; i++)
					{
						var tc = FInput[i] as TouchContainer;
						if(tc.UseFiltered)
						{
							FPointsOut[i] = tc.PointFiltered;
							FVelOut[i] = tc.VelocityFiltered;
						}
						else
						{
							FPointsOut[i] = tc.Point;
							FVelOut[i] = tc.Velocity;
						}
						FIDOut[i] = tc.ID;
						FAge[i] = tc.Age.Elapsed.TotalSeconds;
						FExpiry[i] = tc.ExpireFrames-1;
						FNew[i] = tc.AgeFrames == 1;
					}
				}
			}
			else
			{
				FPointsOut.SliceCount = 0;
				FVelOut.SliceCount = 0;
				FIDOut.SliceCount = 0;
				FAge.SliceCount = 0;
				FExpiry.SliceCount = 0;
				FNew.SliceCount = 0;
			}
		}
	}
}
