using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using md.stdl.Interaction;
using md.stdl.Mathematics;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.Animation;

using VVVV.Core.Logging;
using VVVV.Nodes.PDDN;
using VMatrix = VVVV.Utils.VMath.Matrix4x4;
using SMatrix = System.Numerics.Matrix4x4;

namespace mp.essentials.Nodes.Devices
{
	#region PluginInfo
	[PluginInfo(
        Name = "TouchProcessor",
        Category = "Join",
        Author = "microdee",
        AutoEvaluate = true
        )]
	#endregion PluginInfo
	public class JoinTouchProcessorNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Points")]
		public ISpread<Vector2D> FPoints;
		[Input("ID's")]
		public ISpread<int> FID;
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
        private bool Init = true;
		
		public Dictionary<int, TouchContainer> Touches = new Dictionary<int, TouchContainer>();
		
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

			SpreadMax = Math.Min(FPoints.SliceCount, FID.SliceCount);
			
			var dt = FHDEHost.FrameTime - lastftime;
			lastftime = FHDEHost.FrameTime;

		    foreach (var touch in Touches.Values)
		    {
		        touch.Mainloop();
		    }
			
			for(int i=0; i < SpreadMax; i++)
			{
                int tid = FID[i];
				if(Touches.ContainsKey(tid))
				{
					TouchContainer tc = Touches[tid];
                    if(tc is FilteredTouch ftc)
                    {
                        ftc.FilterMinCutoff = FMinCutoff[i];
                        ftc.FilterBeta = FBeta[i];
                        ftc.FilterCutoffDerivative = FCutoffDerivative[i];
                    }
				    tc.Update(FPoints[i].AsSystemVector(), (float)dt);
                }
				else
				{
					TouchContainer tc = FUseFilter[i] ? new FilteredTouch(tid, FMinCutoff[i], FBeta[i]) : new TouchContainer(tid);
                    tc.Mainloop();
				    if (FUseFilter[i])
				    {
				        var ftc = (FilteredTouch) tc;
				        ftc.FilterCutoffDerivative = FCutoffDerivative[i];
                    }
				    tc.Update(FPoints[i].AsSystemVector(), (float)dt);
                    Touches.Add(tid, tc);
				}
			}

            (from touch in Touches.Values where touch.ExpireFrames > FExpire[0] select touch.Id).ForEach(tid => Touches.Remove(tid));
			
            this.SetSliceCountForAllOutput(Touches.Count);
		    int ii = 0;
		    foreach (var touch in Touches.Values)
		    {
		        FContainer[ii] = touch;
		        FPointsOut[ii] = touch.Point.AsVVector();
		        FVelOut[ii] = touch.Velocity.AsVVector();
		        FIDOut[ii] = touch.Id;
		        FAge[ii] = touch.Age.Elapsed.TotalSeconds;
		        FExpiry[ii] = touch.ExpireFrames;
		        FNew[ii] = touch.AgeFrames < 1;
		        ii++;
		    }
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "TouchProcessor", Category = "Split")]
	#endregion PluginInfo
	public class SplitTouchProcessorNode : ObjectSplitNode<TouchContainer>
	{
	    public override Type TransformType(Type original, MemberInfo member)
	    {
	        if (original == typeof(Vector2))
	        {
	            return typeof(Vector2D);
	        }
	        if (original == typeof(Vector3))
	        {
	            return typeof(Vector3D);
	        }
	        if (original == typeof(Vector4))
	        {
	            return typeof(Vector4D);
	        }
	        if (original == typeof(SMatrix))
	        {
	            return typeof(VMatrix);
	        }
            return original;
	    }

	    public override object TransformOutput(object obj, MemberInfo member, int i)
	    {
	        switch (obj)
	        {
	            case Vector2 v:
	            {
	                return v.AsVVector();
	            }
	            case Vector3 v:
	            {
	                return v.AsVVector();
	            }
	            case Vector4 v:
	            {
	                return v.AsVVector();
	            }
	            case SMatrix v:
	            {
	                return v.AsVMatrix4X4();
	            }
	            default:
	            {
	                return obj;
	            }
            }
        }
	}
}
