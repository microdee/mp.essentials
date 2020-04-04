using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
using md.stdl.Coding;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.devices.Hid
{
    public static class HidChange
    {
        public static int GlobalChangeCounter { get; set; }
    }

    [PluginInfo(
        Name = "OpenDevices",
        Category = "HID",
        Help = "Open potentially composite HID devices based on their VID and PID"
    )]
    public class OpenDevicesHidNode : IPluginEvaluate, IDisposable
    {
        [Input("Vendor ID", AutoValidate = false)]
        protected IDiffSpread<int> VendorIdIn;

        [Input("Product ID", AutoValidate = false)]
        protected IDiffSpread<int> ProductIdIn;

        [Input("Open", IsBang = true)]
        protected ISpread<bool> OpenIn;

        [Input("Close", IsBang = true)]
        protected ISpread<bool> CloseIn;

        [Output("Devices")]
        protected ISpread<HidDeviceCollection> DevicesOut;

        private readonly List<(int vid, int pid)> _removables = new List<(int, int)>();
        private readonly ConcurrentDictionary<(int vid, int pid), HidDeviceCollection> _devices = new ConcurrentDictionary<(int, int), HidDeviceCollection>();

        private int _changeRef;

        public void Evaluate(int SpreadMax)
        {
            if (OpenIn.Any(v => v) || CloseIn.Any(v => v))
            {
                VendorIdIn.Sync();
                ProductIdIn.Sync();
                DevicesOut.SliceCount = SpreadUtils.SpreadMax(ProductIdIn, VendorIdIn);
                _removables.AddRange(_devices.Keys);
                for (int i = 0; i < DevicesOut.SliceCount; i++)
                {
                    var candidate = (VendorIdIn[i], ProductIdIn[i]);
                    if (OpenIn[i])
                    {
                        _removables.Remove(candidate);
                        var device = _devices.AddOrUpdate(candidate, k => new HidDeviceCollection
                        {
                            ProductId = k.pid,
                            VendorId = k.vid
                        }.GetDevice(), (k, d) => d.Open());
                        DevicesOut[i] = device;
                    }

                    if (CloseIn[i] && _devices.TryGetValue(candidate, out var dev))
                    {
                        dev.Close();
                    }
                }

                foreach (var removable in _removables)
                {
                    _devices.TryRemove(removable, out var dev);
                    dev?.Dispose();
                }
                DevicesOut.Stream.IsChanged = true;
                DevicesOut.Flush(true);
            }

            if (_changeRef != HidChange.GlobalChangeCounter)
            {
                _changeRef = HidChange.GlobalChangeCounter;
                DevicesOut.Stream.IsChanged = true;
                DevicesOut.Flush(true);
            }
        }

        public void Dispose()
        {
            _devices.ForeachConcurrent((k, v) => v.Dispose());
        }
    }

    [PluginInfo(
        Name = "OpenDevice",
        Category = "HID",
        Help = "Open a single HID device"
    )]
    public class OpenDeviceHidNode : IPluginEvaluate, IDisposable
    {
        [Input("Hid Device", AutoValidate = false)]
        protected IDiffSpread<HidDevice> DeviceIn;

        [Input("Open", IsBang = true)]
        protected ISpread<bool> OpenIn;

        [Input("Close", IsBang = true)]
        protected ISpread<bool> CloseIn;

        [Input("Open on Init")]
        protected ISpread<bool> OpenOnInitIn;

        [Output("Output")]
        protected ISpread<HidDeviceWrap> DeviceOut;

        private int _changeRef;

        public void Evaluate(int SpreadMax)
        {
            if (OpenIn.Any(v => v) || CloseIn.Any(v => v))
            {
                DeviceIn.Sync();
                DeviceOut.ResizeAndDispose(DeviceIn.SliceCount, () => new HidDeviceWrap());
                for (int i = 0; i < DeviceOut.SliceCount; i++)
                {
                    if (OpenIn[i])
                    {
                        if (DeviceOut[i] == null) DeviceOut[i] = new HidDeviceWrap();
                        if (DeviceOut[i].Device == null)
                        {
                            DeviceOut[i].Device = DeviceIn[i];
                            DeviceOut[i].Initialize();
                        }
                        else
                        {
                            DeviceOut[i].Open();
                        }
                    }

                    if (CloseIn[i] && DeviceOut[i] != null && DeviceOut[i].Opened)
                    {
                        DeviceOut[i].Close();
                        DeviceOut[i].Dispose();
                        DeviceOut[i] = null;
                    }
                }
                DeviceOut.Stream.IsChanged = true;
                DeviceOut.Flush(true);
            }

            if (_changeRef != HidChange.GlobalChangeCounter)
            {
                _changeRef = HidChange.GlobalChangeCounter;
                DeviceOut.Stream.IsChanged = true;
                DeviceOut.Flush(true);
            }
        }

        public void Dispose()
        {
            foreach (var wrap in DeviceOut)
            {
                wrap.Dispose();
            }
        }
    }
}
