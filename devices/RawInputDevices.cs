using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;

namespace mp.essentials.devices
{
    [PluginInfo(
        Name = "Mice",
        Category = "Mouse",
        Version = "Desktop",
        Author = "microdee",
        Help = "List all raw mouse devices and info about them"
    )]
    public class DesktopMiceNode : RawInputDeviceNode<MouseInputManager, Mouse> { }

    [PluginInfo(
        Name = "Keyboards",
        Category = "Keyboard",
        Version = "Desktop",
        Author = "microdee",
        Help = "List all raw keyboard devices and info about them"
    )]
    public class DesktopKeyboardsNode : RawInputDeviceNode<KeyboardInputManager, Keyboard> { }

    public abstract class RawInputDeviceNode<TManager, TDevice> : IPluginEvaluate, IPartImportsSatisfiedNotification
        where TManager : DesktopDeviceInputManager<TDevice>, new()
    {
        private TManager _manager;

        [Output("Output")]
        public ISpread<TDevice> DeviceOut;

        [Output("Name")]
        public ISpread<string> NameOut;

        [Output("SubClass")]
        public ISpread<string> SubClassOut;

        [Output("Has HID info")]
        public ISpread<bool> HasHidOut;

        [Output("Vid")]
        public ISpread<int> VidOut;

        [Output("Pid")]
        public ISpread<int> PidOut;

        public void OnImportsSatisfied()
        {
            _manager = new TManager();
            _manager.SelectDevice(false, (info, i) => true);
            _manager.DeviceListChanged += (sender, args) => UpdateDevices();
            UpdateDevices();
        }

        private void UpdateDevices()
        {
            DeviceOut.SliceCount = NameOut.SliceCount = SubClassOut.SliceCount =
                VidOut.SliceCount = PidOut.SliceCount = HasHidOut.SliceCount = _manager.WrappedDevices.Count;

            for (int i = 0; i < _manager.WrappedDevices.Count; i++)
            {
                var wrap = _manager.WrappedDevices[i];
                DeviceOut[i] = wrap.Device;
                NameOut[i] = wrap.Name;
                var desc = wrap.Info.GetDeviceDescription();
                SubClassOut[i] = desc.SubclassCode;
                HasHidOut[i] = wrap.Info.GetVidPid(out var vid, out var pid);
                VidOut[i] = vid;
                PidOut[i] = pid;
            }
        }

        public void Evaluate(int SpreadMax)
        { }
    }
}
