#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class StateServer
	{
		public List<string> States;
		public string CurrentState = "";
		public bool SetState = false;
		public string SetOrigin = "";
		public int StateIndex = 0;
		public StateServer()
		{
			this.States = new List<string>();
		}
	}
	#region PluginInfo
	[PluginInfo(Name = "StateServer", Category = "States", Tags = "")]
	#endregion PluginInfo
	public class StatesStateServerNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("State List")]
		public IDiffSpread<string> FStates;
		
		[Output("Output")]
		public ISpread<StateServer> FOutput;
		[Output("Current State")]
		public ISpread<string> FCurrent;
		[Output("Set Origin")]
		public ISpread<string> FSetOrigin;
		[Output("StateIndex")]
		public ISpread<int> FID;

		StateServer Everything;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		[ImportingConstructor()]
		public StatesStateServerNode()
		{
			this.Everything = new StateServer();
		}
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = 1;
			FCurrent.SliceCount = 1;
			
			if(FStates.IsChanged)
			{
				Everything.States.Clear();
				for(int i=0; i<FStates.SliceCount; i++)
				{
					Everything.States.Add(FStates[i]);
				}
			}
			if(Everything.SetState)
			{
				bool foundstate = false;
				for(int i=0; i<Everything.States.Count; i++)
				{
					if(Everything.States[i]==Everything.CurrentState)
					{
						foundstate = true;
						Everything.StateIndex = i;
					}
				}
				if(foundstate)
				{
					FCurrent[0] = Everything.CurrentState;
					FID[0] = Everything.StateIndex;
					FSetOrigin[0] = Everything.SetOrigin;
				}
			}
			if(Everything.CurrentState=="")
			{
				Everything.CurrentState = Everything.States[0];
				FCurrent[0] = Everything.States[0];
			}
			FOutput[0] = Everything;
			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "SetState", Category = "States", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class StatesSetStateNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("State Server", IsSingle = true)]
		public IDiffSpread<StateServer> FServer;
		[Input("Current State", IsSingle = true)]
		public ISpread<string> FState;
		[Input("Set Origin", IsSingle = true)]
		public ISpread<string> FSetOrigin;
		[Input("Set", IsSingle = true, IsBang = true)]
		public IDiffSpread<bool> FSet;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FSet[0])
			{
				FServer[0].CurrentState = FState[0];
				FServer[0].SetOrigin = FSetOrigin[0];
			}
			if(FSet.IsChanged) FServer[0].SetState = FSet[0];
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "GetStates", Category = "States", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoManagerSiftNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Server")]
		public IDiffSpread<StateServer> FServer;

		[Output("States")]
		public ISpread<string> FStates;
		[Output("Current State")]
		public ISpread<string> FCState;
		[Output("StateIndex")]
		public ISpread<int> FID;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FStates.SliceCount = FServer[0].States.Count;
			FCState.SliceCount = 1;
			FCState[0] = FServer[0].CurrentState;
			FID[0] = FServer[0].StateIndex;
			for(int i=0; i<FServer[0].States.Count; i++) FStates[i] = FServer[0].States[i];
		}
	}
}