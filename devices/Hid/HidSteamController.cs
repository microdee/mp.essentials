using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
using md.stdl.StreamUtils;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.devices.Hid
{
    public class HidSteamController
    {
        public bool Idle { get; private set; }
        public bool Active { get; private set; }
        public float ActiveTime { get; private set; }
        public int IdleTime { get; private set; }
        public int BatteryMillivolts { get; private set; }
        public float BatteryNormalized { get; private set; }
        public float TriggerLeft { get; private set; }
        public bool TriggerPressedLeft { get; private set; }
        public float TriggerRight { get; private set; }
        public bool TriggerPressedRight { get; private set; }
        public bool ShoulderLeft { get; private set; }
        public bool ShoulderRight { get; private set; }
        public bool A { get; private set; }
        public bool B { get; private set; }
        public bool X { get; private set; }
        public bool Y { get; private set; }
        public Vector2D TouchpadLeft { get; private set; }
        public bool TouchpadTouchedLeft { get; private set; }
        public bool TouchpadPressedLeft { get; private set; }
        public Vector2D TouchpadRight { get; private set; }
        public bool TouchpadTouchedRight { get; private set; }
        public bool TouchpadPressedRight { get; private set; }
        public bool GripLeft { get; private set; }
        public bool GripRight { get; private set; }
        public bool Back { get; private set; }
        public bool SteamButton { get; private set; }
        public bool Start { get; private set; }
        public Vector2D Analog { get; private set; }
        public bool AnalogPressed { get; private set; }
        public Vector3D Acceleration { get; private set; }
        public Vector3D AngularVelocity { get; private set; }
        public Vector4D Orientation { get; private set; }
        public Vector4D OrientationVvvv { get; private set; }

        public Exception Update(Stream input)
        {
            try
            {
                input.Position = 3;
                byte status = (byte) input.ReadByte();
                Active = (status & 0b_00000001) > 0;
                Idle = (status & 0b_00000100) > 0;

                input.Position = 5;
                if (!Idle)
                {
                    int ats0 = input.ReadByte();
                    int ats1 = input.ReadByte();
                    ActiveTime = ((float) ats0) / 256 + ats1;
                }
                else
                {
                    IdleTime = input.ReadUshort();
                }

                if (!Idle)
                {
                    // buttons
                    input.Position = 9;
                    byte b0 = (byte)input.ReadByte();
                    TriggerPressedRight = (b0 & 0b_00000001) > 0;
                    TriggerPressedLeft = (b0 & 0b_00000010) > 0;
                    ShoulderRight = (b0 & 0b_00000100) > 0;
                    ShoulderLeft = (b0 & 0b_00001000) > 0;
                    A = (b0 & 0b_10000000) > 0;
                    B = (b0 & 0b_00100000) > 0;
                    X = (b0 & 0b_01000000) > 0;
                    Y = (b0 & 0b_00010000) > 0;

                    // p = 10
                    byte b1 = (byte)input.ReadByte();
                    GripLeft = (b1 & 0b_10000000) > 0;
                    Start = (b1 & 0b_01000000) > 0;
                    SteamButton = (b1 & 0b_00100000) > 0;
                    Back = (b1 & 0b_00010000) > 0;

                    // p = 11
                    byte b2 = (byte)input.ReadByte();
                    GripRight = (b2 & 0b_00000001) > 0;
                    bool tpl0 = (b2 & 0b_00000010) > 0;
                    bool tpl1 = (b2 & 0b_00001000) > 0;
                    bool tpl2 = (b2 & 0b_10000000) > 0;
                    TouchpadTouchedLeft = tpl1 || tpl2;
                    TouchpadPressedLeft = TouchpadTouchedLeft && tpl0;
                    TouchpadTouchedRight = (b2 & 0b_00010000) > 0;
                    TouchpadPressedRight = (b2 & 0b_00000100) > 0;
                    AnalogPressed = (b2 & 0b_01000000) > 0;

                    // Triggers
                    // p = 12
                    TriggerLeft = ((float)input.ReadByte()) / 255.0f;
                    TriggerRight = ((float)input.ReadByte()) / 255.0f;

                    // analog
                    input.Position = 17;
                    float aX = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float aY = ((float)input.ReadShort()) / ((float)0x7FFF);

                    var rot = -0.04 * VMath.CycToRad;
                    if (tpl1)
                        TouchpadLeft = new Vector2D(
                            aX * Math.Cos(rot) - aY * Math.Sin(rot),
                            aX * Math.Sin(rot) + aY * Math.Cos(rot)
                        );
                    else
                        Analog = new Vector2D(aX, aY);

                    float bX = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float bY = ((float)input.ReadShort()) / ((float)0x7FFF);

                    if(TouchpadTouchedRight)
                        TouchpadRight = new Vector2D(
                            bX * Math.Cos(-rot) - bY * Math.Sin(-rot),
                            bX * Math.Sin(-rot) + bY * Math.Cos(-rot)
                        );

                    // IMU
                    input.Position = 29;
                    float accX = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float accY = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float accZ = ((float)input.ReadShort()) / ((float)0x7FFF);

                    Acceleration = new Vector3D(accX, -accZ, accY);

                    float angvX = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float angvY = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float angvZ = ((float)input.ReadShort()) / ((float)0x7FFF);

                    AngularVelocity = new Vector3D(angvX, angvY, angvZ);

                    float quatX = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float quatY = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float quatZ = ((float)input.ReadShort()) / ((float)0x7FFF);
                    float quatW = ((float)input.ReadShort()) / ((float)0x7FFF);

                    Orientation = new Vector4D(quatX, quatY, quatZ, quatW);
                    OrientationVvvv = new Vector4D(-quatY, -quatW, -quatZ, quatX);
                }
                else
                {
                    input.Position = 13;
                    BatteryMillivolts = input.ReadUshort();
                    BatteryNormalized = (float)BatteryMillivolts / 3000.0f;
                }

                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }

    [PluginInfo(
        Name = "SteamControllerSplit",
        Category = "HID",
        Author = "microdee",
        Help = "Splits the parsed steam controller"
    )]
    public class HidSteamControllerSplitNode : ObjectSplitNode<HidSteamController> { }

    [PluginInfo(
        Name = "SteamControllerParse",
        Category = "HID",
        Author = "microdee",
        Help = "Splits the parsed steam controller"
    )]
    public class HidSteamControllerParseNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<Stream> Input;

        [Output("Output")]
        public ISpread<HidSteamController> Output;

        [Output("Error")]
        public ISpread<Exception> ErrorOut;
        public void Evaluate(int SpreadMax)
        {
            if (!Input.IsConnected)
            {
                Output.SliceCount = 0;
                ErrorOut.SliceCount = 0;
                return;
            }

            Output.ResizeAndDismiss(Input.SliceCount, () => new HidSteamController());
            Output.Stream.IsChanged = true;
            ErrorOut.SliceCount = Output.SliceCount;

            for (int i = 0; i < Output.SliceCount; i++)
            {
                ErrorOut[i] = Output[i].Update(Input[i]);
            }
        }
    }

    [PluginInfo(
        Name = "SteamControllerInitData",
        Category = "HID",
        Author = "microdee"
    )]
    public class HidSteamControllerInitNode : IPluginEvaluate
    {
        [Output("Output")]
        public ISpread<Stream> Output;
        public void Evaluate(int SpreadMax)
        {
            Output.SliceCount = 1;
            if (Output[0] == null)
            {
                Output[0] = new MemoryStream(
                    new byte[]{ 0, 135, 15, 48, 60, 0, 46, 0, 0, 53, 10, 0, 52, 10, 0, 59, 10}
                        .Concat(Enumerable.Repeat((byte)0, 48))
                        .ToArray()
                );
            }
        }
    }

    [PluginInfo(
        Name = "SteamControllerRumble",
        Category = "HID",
        Author = "microdee"
    )]
    public class HidSteamControllerRumbleNode : IPluginEvaluate
    {
        [Input("Side (Left/Right)")]
        public IDiffSpread<bool> SideIn;
        [Input("Intensity")]
        public IDiffSpread<int> IntensityIn;
        [Input("Period")]
        public IDiffSpread<int> PeriodIn;
        [Input("Count")]
        public IDiffSpread<int> CountIn;
        [Input("Rumble", IsBang = true)]
        public IDiffSpread<bool> RumbleIn;

        [Output("Output")]
        public ISpread<Stream> Output;
        public void Evaluate(int SpreadMax)
        {
            Output.SliceCount = SpreadMax;
            //bool changed = SpreadUtils.AnyChanged(SideIn, IntensityIn, PeriodIn, CountIn);
            //if(!changed) return;

            for (int i = 0; i < SpreadMax; i++)
            {
                if (!RumbleIn[i])
                {
                    Output[i] = null;
                    continue;
                }
                var o = new MemoryStream(Enumerable.Repeat((byte)0, 65).ToArray());
                o.Position = 0;
                o.WriteByte(0);
                o.WriteByte(143);
                o.WriteByte(7);
                o.WriteByte((byte)(SideIn[0] ? 0 : 1));
                o.WriteUshort((ushort)IntensityIn[i]);
                o.WriteUshort((ushort)PeriodIn[i]);
                o.WriteUshort((ushort)CountIn[i]);
                Output[i] = o;
            }
        }
    }
}
