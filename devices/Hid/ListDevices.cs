using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.devices.Hid
{
    [PluginInfo(
        Name = "ListDevices",
        Category = "HID",
        Help = "Get all HID devices"
    )]
    public class ListHidDevices : IPluginEvaluate
    {

        [Input(
            "Vendor ID",
            DefaultValue = -1
        )]
        protected IDiffSpread<int> VendorIdIn;

        [Input(
            "Product ID",
            DefaultValue = -1
        )]
        protected IDiffSpread<int> ProductIdIn;

        [Output("Devices")]
        protected ISpread<ISpread<HidDevice>> DevicesOut;

        [Output("Changed", IsBang = true)]
        protected ISpread<bool> ChangedOut;

        private bool _init = true;
        private bool _devChange = true;

        public void Evaluate(int SpreadMax)
        {
            if (_init)
            {
                DeviceList.Local.Changed += (sender, args) => _devChange = true;
                _init = false;
            }

            if (_devChange || SpreadUtils.AnyChanged(VendorIdIn, ProductIdIn))
            {
                var sprmax = SpreadUtils.SpreadMax(VendorIdIn, ProductIdIn);
                DevicesOut.SliceCount = sprmax;

                for (int i = 0; i < sprmax; i++)
                {
                    if (VendorIdIn[i] < 0 || ProductIdIn[i] < 0)
                    {
                        var devices = DeviceList.Local.GetHidDevices();
                        DevicesOut[i].AssignFrom(devices);
                    }
                    else
                    {
                        var devices = DeviceList.Local.GetHidDevices(VendorIdIn[i], ProductIdIn[i]);
                        DevicesOut[i].AssignFrom(devices);
                    }
                }

                _devChange = false;
            }

            ChangedOut[0] = _devChange;
        }
    }
}
