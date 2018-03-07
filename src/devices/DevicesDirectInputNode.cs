#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using SlimDX;
using SlimDX.DirectInput;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace mp.essentials.Nodes.Devices
{
	[PluginInfo(
        Name = "GameController",
        Category = "Devices",
        Version = "DirectInput",
        Tags = "Joystick, Gamepad, Analog",
        Author = "microdee"
        )]
	public class DirectInputDevicesGameControllerNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Window Handle", DefaultValue = -1, IsSingle = true)]
		public ISpread<int> FHandle;
		[Input("Foreground", DefaultValue = 0, IsSingle = true)]
		public ISpread<bool> FFrg;
		[Input("Exclusive", DefaultValue = 0, IsSingle = true)]
		public ISpread<bool> FExclusive;
		[Input("Reinitialize", DefaultValue = 0, IsBang = true, IsSingle = true)]
		public ISpread<bool> FInit;
		[Input("Device", EnumName = "DirectInputGameControllerDevices", IsSingle = true)]
		public IDiffSpread<EnumEntry> FDevice;

		[Output("Name")]
		public ISpread<string> FName;
		
		[Output("XYZ ")]
		public ISpread<Vector3D> FXYZ;
		[Output("Velocity ")]
		public ISpread<Vector3D> Fv;
		[Output("Acceleration ")]
		public ISpread<Vector3D> Fa;
		
		[Output("Rotation ")]
		public ISpread<Vector3D> FRot;
		[Output("Angular Velocity ")]
		public ISpread<Vector3D> FAv;
		[Output("Angular Acceleration ")]
		public ISpread<Vector3D> FAa;
		
		[Output("Torque ")]
		public ISpread<Vector3D> Ft;
		[Output("Force ")]
		public ISpread<Vector3D> Ff;
		
		[Output("Sliders")]
		public ISpread<ISpread<int>> FSliders;
		[Output("Point of View")]
		public ISpread<ISpread<int>> FPoV;
		
		[Output("Buttons")]
		public ISpread<ISpread<bool>> FButtons;
		
		[Output("Objects")]
		public ISpread<ISpread<DeviceObjectInstance>> FObjects;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		[DllImport("C:\\Windows\\System32\\user32.dll")]
		public static extern IntPtr GetForegroundWindow();
		
		private DirectInput dinput = new DirectInput();
		private List<Joystick> Joysticks = new List<Joystick>();
		private bool init = true;
		
		private void InitDevice() {
			Joysticks.Clear();
			
			foreach(DeviceInstance di in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
			{
				if(FDevice[0].Name == "All" || FDevice[0].Name == di.InstanceName)
					Joysticks.Add(new Joystick(dinput, di.InstanceGuid));
			}
			
			IntPtr handle = (FHandle[0]==-1) ? GetForegroundWindow() : new IntPtr(FHandle[0]);
			if(FFrg[0] && (!FExclusive[0])) {
				foreach(Joystick J in Joysticks)
				{
					J.SetCooperativeLevel(
						handle,
						CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive
					);
				}
			}
			if(FFrg[0] && FExclusive[0]) {
				foreach(Joystick J in Joysticks)
				{
					J.SetCooperativeLevel(
						handle,
						CooperativeLevel.Foreground | CooperativeLevel.Exclusive
					);
				}
			}
			if(!FFrg[0]) {
				foreach(Joystick J in Joysticks)
				{
					J.SetCooperativeLevel(
						handle,
						CooperativeLevel.Background | CooperativeLevel.Nonexclusive
					);
				}
			}
			foreach(Joystick J in Joysticks)
			{
				J.Acquire();
			}
		}
		
		private void UpdateDeviceList()
		{
			var s = new string[]{"All"};
		    EnumManager.UpdateEnum("DirectInputGameControllerDevices", "All", s);
			
			foreach(DeviceInstance di in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
			{
				EnumManager.AddEntry("DirectInputGameControllerDevices", di.InstanceName);
			}
		}
		
		[ImportingConstructor]
		public DirectInputDevicesGameControllerNode()
		{ 
			UpdateDeviceList();
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if(FInit[0] || init || FDevice.IsChanged)
			{
				if(FInit[0])
				{
					
				}
				InitDevice();
				init = false;
			}
			
			FName.SliceCount = Joysticks.Count;
			FXYZ.SliceCount = Joysticks.Count;
			Fv.SliceCount = Joysticks.Count;
			Fa.SliceCount = Joysticks.Count;
			FRot.SliceCount = Joysticks.Count;
			FAv.SliceCount = Joysticks.Count;
			FAa.SliceCount = Joysticks.Count;
			Ft.SliceCount = Joysticks.Count;
			Ff.SliceCount = Joysticks.Count;
			FSliders.SliceCount = Joysticks.Count;
			FPoV.SliceCount = Joysticks.Count;
			FButtons.SliceCount = Joysticks.Count;
			FObjects.SliceCount = Joysticks.Count;
			
			for(int i=0; i<Joysticks.Count; i++)
			{
				Joystick J = Joysticks[i];
				JoystickState Js = J.GetCurrentState();
				
				FName[i] = J.Information.InstanceName;
				FXYZ[i] = new Vector3D(Js.X, Js.Y, Js.Z);
				Fv[i] = new Vector3D(Js.VelocityX, Js.VelocityY, Js.VelocityZ);
				Fa[i] = new Vector3D(Js.AccelerationX, Js.AccelerationY, Js.AccelerationZ);
				FRot[i] = new Vector3D(Js.RotationX, Js.RotationY, Js.RotationZ);
				FAv[i] = new Vector3D(Js.AngularVelocityX, Js.AngularVelocityY, Js.AngularVelocityZ);
				FAa[i] = new Vector3D(Js.AngularAccelerationX, Js.AngularAccelerationY, Js.AngularAccelerationZ);
				Ft[i] = new Vector3D(Js.TorqueX, Js.TorqueY, Js.TorqueZ);
				Ff[i] = new Vector3D(Js.ForceX, Js.ForceY, Js.ForceZ);
				
				int[] sliders = Js.GetSliders();
				FSliders[i].SliceCount = sliders.Length;
				for(int j=0; j<sliders.Length; j++)
				{
					FSliders[i][j] = sliders[j];
				}
				
				int[] pov = Js.GetPointOfViewControllers();
				FPoV[i].SliceCount = pov.Length;
				for(int j=0; j<pov.Length; j++)
				{
					FPoV[i][j] = pov[j];
				}
				
				bool[] buttons = Js.GetButtons();
				FButtons[i].SliceCount = buttons.Length;
				for(int j=0; j<buttons.Length; j++)
				{
					FButtons[i][j] = buttons[j];
				}
				
				IList<DeviceObjectInstance> objs = J.GetObjects();
				FObjects[i].SliceCount = objs.Count;
				for(int j=0; j<objs.Count; j++)
				{
					FObjects[i][j] = objs[j];
				}
			}
		}
	}

	[PluginInfo(
        Name = "DeviceObject",
        Category = "Devices",
        Version = "DirectInput",
        Author = "microdee"
        )]
	public class DirectInputDevicesDeviceObjectNode : IPluginEvaluate
	{
		[Input("Device Object")]
		public ISpread<DeviceObjectInstance> FDOI;
		
		[Output("Collection Number")]
		public ISpread<int> FCollectionNumber;
		[Output("Designator Index")]
		public ISpread<int> FDesignatorIndex;
		[Output("Dimension")]
		public ISpread<int> FDimension;
		[Output("Exponent")]
		public ISpread<int> FExponent;
		[Output("Force Feedback Resolution")]
		public ISpread<int> FFFR;
		[Output("Maximum Force Feedback")]
		public ISpread<int> FMFF;
		[Output("Name")]
		public ISpread<string> FName;
		[Output("Object Type")]
		public ISpread<string> FType;
		[Output("Offset")]
		public ISpread<int> FOffset;
		[Output("Report ID")]
		public ISpread<int> FReportID;
		[Output("Usage")]
		public ISpread<int> FUsage;
		[Output("Usage Page")]
		public ISpread<int> FUsagePage;
		
		public void Evaluate(int SpreadMax)
		{
			FCollectionNumber.SliceCount = FDOI.SliceCount;
			FDesignatorIndex.SliceCount = FDOI.SliceCount;
			FDimension.SliceCount = FDOI.SliceCount;
			FExponent.SliceCount = FDOI.SliceCount;
			FFFR.SliceCount = FDOI.SliceCount;
			FMFF.SliceCount = FDOI.SliceCount;
			FName.SliceCount = FDOI.SliceCount;
			FType.SliceCount = FDOI.SliceCount;
			FOffset.SliceCount = FDOI.SliceCount;
			FReportID.SliceCount = FDOI.SliceCount;
			FUsage.SliceCount = FDOI.SliceCount;
			FUsagePage.SliceCount = FDOI.SliceCount;
			
			for(int i=0; i<FDOI.SliceCount; i++)
			{
				FCollectionNumber[i] = FDOI[i].CollectionNumber;
				FDesignatorIndex[i] = FDOI[i].DesignatorIndex;
				FDimension[i] = FDOI[i].Dimension;
				FExponent[i] = FDOI[i].Exponent;
				FFFR[i] = FDOI[i].ForceFeedbackResolution;
				FMFF[i] = FDOI[i].MaximumForceFeedback;
				FName[i] = FDOI[i].Name;
				FType[i] = FDOI[i].ObjectType.ToString();
				FOffset[i] = FDOI[i].Offset;
				FReportID[i] = FDOI[i].ReportId;
				FUsage[i] = FDOI[i].Usage;
				FUsagePage[i] = FDOI[i].UsagePage;
			}
		}
	}
}
