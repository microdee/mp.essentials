using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.windows
{
    public enum MonitorDpiType
    {
        MDT_DEFAULT = 0,
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
    }

    public enum MonitorFlag
    {
        MONITOR_DEFAULTTONULL,
        MONITOR_DEFAULTTOPRIMARY,
        MONITOR_DEFAULTTONEAREST,
    }

    public class Monitor
    {
        public Screen Screen;
        public IntPtr Handle;
        public Vector4D Bounds;
        public Vector4D WorkingArea;
        public Vector2D EffectiveDpi;
        public Vector2D RawDpi;
        public Vector2D ScaleFactor;
        public Vector2D PrimaryRelativeScaleFactor;
    }

    [PluginInfo(
        Name = "Monitor",
        Category = "ScreenInfo",
        Author = "microdee"
    )]
    public class MonitorSplitNode : ObjectSplitNode<Monitor>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            if (original == typeof(IntPtr)) return typeof(long);
            return base.TransformType(original, member);
        }

        public override object TransformOutput(object obj, MemberInfo member, int i)
        {
            if (obj is IntPtr cobj) return cobj.ToInt64();
            return base.TransformOutput(obj, member, i);
        }
    }
    
    [PluginInfo(
        Name = "ScreenInfo",
        Category = "Windows",
        Version = "Advanced",
        Tags = "dpi aware",
        Help = "DPI Aware version of ScreenInfo",
        Author = "microdee"
    )]
    public class WindowsScreenInfoNode : IPluginEvaluate
    {
        [Input("Refresh", IsBang = true)]
        public ISpread<bool> RefreshIn;
        [Input("Divide by Primary DPI Scaling", DefaultBoolean = true)]
        public IDiffSpread<bool> DivideByPrimaryDpiScalingIn;
        [Input("Order Right-to-Left", DefaultBoolean = true)]
        public IDiffSpread<bool> RtlOrderIn;

        [Output("Monitor")]
        public ISpread<Monitor> MonitorOut;

        [Output("Bounds ", DimensionNames = new []{"L", "T", "W", "H"})]
        public ISpread<Vector4D> BoundsOut;
        [Output("WorkingArea ", DimensionNames = new[] { "L", "T", "W", "H" })]
        public ISpread<Vector4D> WorkingAreaOut;

        [Output("Device Name")]
        public ISpread<string> DeviceNameOut;
        [Output("Is Primary")]
        public ISpread<bool> IsPrimaryOut;

        [Output("Effective Dpi ")]
        public ISpread<Vector2D> EffectiveDpiOut;
        [Output("Raw Dpi ")]
        public ISpread<Vector2D> RawDpiOut;
        [Output("Scale Factor ")]
        public ISpread<Vector2D> ScaleFactorOut;
        [Output("Primary Relative Scale Factor ")]
        public ISpread<Vector2D> PrimaryRelativeScaleFactorOut;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetThreadDpiAwarenessContext(int dpiContext);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr MonitorFromPoint(Point point, MonitorFlag monflag);

        [DllImport("shcore.dll", SetLastError = true)]
        public static extern int GetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpix, out uint dpiy);

        private Screen[] _screens;

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (RefreshIn.TryGetSlice(0) || RtlOrderIn.IsChanged || DivideByPrimaryDpiScalingIn.IsChanged)
            {
                var _screens = Screen.AllScreens;
                var t = Task.Run(() =>
                {
                    SetThreadDpiAwarenessContext(-3);
                    var monitors = _screens.Select(screen =>
                    {
                        var monitor = new Monitor { Screen = screen };

                        monitor.Handle = MonitorFromPoint(
                            new Point(screen.WorkingArea.Left + 5, screen.WorkingArea.Top + 5),
                            MonitorFlag.MONITOR_DEFAULTTONULL
                        );
                        GetDpiForMonitor(
                            monitor.Handle,
                            MonitorDpiType.MDT_EFFECTIVE_DPI,
                            out var fdpix, out var fdpiy
                        );
                        GetDpiForMonitor(
                            monitor.Handle,
                            MonitorDpiType.MDT_RAW_DPI,
                            out var rdpix, out var rdpiy
                        );
                        monitor.EffectiveDpi = new Vector2D(fdpix, fdpiy);
                        monitor.RawDpi = new Vector2D(rdpix, rdpiy);
                        monitor.ScaleFactor = new Vector2D(fdpix / 96.0, fdpiy / 96.0);
                        monitor.PrimaryRelativeScaleFactor = monitor.ScaleFactor;

                        monitor.Bounds = new Vector4D(
                            screen.Bounds.Left,
                            screen.Bounds.Top,
                            screen.Bounds.Width * monitor.ScaleFactor.x,
                            screen.Bounds.Height * monitor.ScaleFactor.y
                        );

                        monitor.WorkingArea = new Vector4D(
                            screen.WorkingArea.Left,
                            screen.WorkingArea.Top,
                            screen.WorkingArea.Width,
                            screen.WorkingArea.Height
                        );

                        return monitor;
                    }).ToList();

                    if (RtlOrderIn.TryGetSlice(0))
                    {
                        uint minleft = (uint)-Math.Min(monitors.Min(mon => mon.Screen.WorkingArea.Left), 0);
                        uint mintop = (uint)-Math.Min(monitors.Min(mon => mon.Screen.WorkingArea.Top), 0);
                        monitors = monitors.OrderBy(mon =>
                        {
                            ulong res =
                                ((ulong)(mon.Screen.WorkingArea.Left + minleft) << 32) |
                                (ulong)(mon.Screen.WorkingArea.Top + mintop);
                            return res;
                        }).ToList();
                    }

                    if (DivideByPrimaryDpiScalingIn.TryGetSlice(0))
                    {
                        var primary = monitors.FirstOrDefault(mon => mon.Screen.Primary);
                        if (primary != null)
                        {
                            primary.PrimaryRelativeScaleFactor = new Vector2D(1, 1);
                            var psf = primary.ScaleFactor;
                            foreach (var mon in monitors)
                            {
                                mon.Bounds = new Vector4D(
                                    mon.Bounds.x / psf.x,
                                    mon.Bounds.y / psf.y,
                                    mon.Bounds.z / psf.x,
                                    mon.Bounds.w / psf.y
                                );
                                mon.PrimaryRelativeScaleFactor = mon.ScaleFactor / psf;
                            }
                        }
                    }

                    MonitorOut.AssignFrom(monitors);

                    BoundsOut.AssignFrom(from mon in monitors select mon.Bounds);
                    WorkingAreaOut.AssignFrom(from mon in monitors select mon.WorkingArea);
                    DeviceNameOut.AssignFrom(from mon in monitors select mon.Screen.DeviceName);
                    IsPrimaryOut.AssignFrom(from mon in monitors select mon.Screen.Primary);
                    EffectiveDpiOut.AssignFrom(from mon in monitors select mon.EffectiveDpi);
                    RawDpiOut.AssignFrom(from mon in monitors select mon.RawDpi);
                    ScaleFactorOut.AssignFrom(from mon in monitors select mon.ScaleFactor);
                    PrimaryRelativeScaleFactorOut.AssignFrom(from mon in monitors select mon.PrimaryRelativeScaleFactor);
                });
                t.Wait();
            }
        }
    }
}
