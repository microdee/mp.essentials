#region usings
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public abstract class AsWeakObjectNode<T> : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<T> FInput;

		[Output("Output")]
		public ISpread<object> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			for (int i = 0; i < FInput.SliceCount; i++)
			{
				FOutput[i] = (object)FInput[i];
			}
		}
	}
	
	public abstract class AsStrongObjectNode<T> : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input")]
		public ISpread<object> FInput;

		[Output("Output")]
		public ISpread<ISpread<T>> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FInput.SliceCount;
			for (int i = 0; i < FInput.SliceCount; i++)
			{
				if(FInput[i] is T)
				{
					FOutput[i].SliceCount = 1;
					FOutput[i][0] = (T)FInput[i];
				}
				if(FInput[i] is IEnumerable<T>)
				{
					var tc = FInput[i] as IEnumerable<T>;
					FOutput[i].SliceCount = 0;
					foreach(T o in tc)
					{
						FOutput[i].Add(o);
					}
				}
				if(FInput[i] is IEnumerable<object>)
				{
					var tc = FInput[i] as IEnumerable<object>;
					var ta = tc.ToArray();
					if(ta.Length > 0)
					{
						if(ta[0] is T)
						{
							FOutput[i].SliceCount = ta.Length;
							int j=0;
							foreach(object o in ta)
							{
								FOutput[i][j] = (T)o;
								j++;
							}
						}
					}
				}
			}
		}
	}

	// Miss a type? Can you see the pattern here? ;)

	[PluginInfo(Name = "AsWeakObject", Category = "Value", Tags = "")]
	public class ValueAsWeakObjectNode : AsWeakObjectNode<double> { }
	[PluginInfo(Name = "AsStrongObject", Category = "Value", Tags = "")]
	public class ValueAsStrongObjectNode : AsStrongObjectNode<double> { }
	
	[PluginInfo(Name = "AsWeakObject", Category = "2d", Tags = "")]
	public class Vector2DAsWeakObjectNode : AsWeakObjectNode<Vector2D> { }
	[PluginInfo(Name = "AsStrongObject", Category = "2d", Tags = "")]
	public class Vector2DAsStrongObjectNode : AsStrongObjectNode<Vector2D> { }
	
	[PluginInfo(Name = "AsWeakObject", Category = "3d", Tags = "")]
	public class Vector3DAsWeakObjectNode : AsWeakObjectNode<Vector3D> { }
	[PluginInfo(Name = "AsStrongObject", Category = "3d", Tags = "")]
	public class Vector3DAsStrongObjectNode : AsStrongObjectNode<Vector3D> { }
	
	[PluginInfo(Name = "AsWeakObject", Category = "4d", Tags = "")]
	public class Vector4DAsWeakObjectNode : AsWeakObjectNode<Vector4D> { }
	[PluginInfo(Name = "AsStrongObject", Category = "4d", Tags = "")]
	public class Vector4DAsStrongObjectNode : AsStrongObjectNode<Vector4D> { }

	[PluginInfo(Name = "AsWeakObject", Category = "Transform", Tags = "")]
	public class TransformAsWeakObjectNode : AsWeakObjectNode<Matrix4x4> { }
	[PluginInfo(Name = "AsStrongObject", Category = "Transform", Tags = "")]
	public class TransformAsStrongObjectNode : AsStrongObjectNode<Matrix4x4> { }
	
	[PluginInfo(Name = "AsWeakObject", Category = "String", Tags = "")]
	public class StringAsWeakObjectNode : AsWeakObjectNode<string> { }
	[PluginInfo(Name = "AsStrongObject", Category = "String", Tags = "")]
	public class StringAsStrongObjectNode : AsStrongObjectNode<string> { }
}
