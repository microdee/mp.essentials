using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using md.stdl.Coding;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.windows
{
    [PluginInfo(
        Name = "SetAudioDeviceVolume",
        Category = "System",
        Version = "Audio"
    )]
    public class SetAudioVolumeNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Volume", DefaultValue = 1.0)]
        public IDiffSpread<double> Volume;

        [Input("Device", DefaultEnumEntry = "Default", EnumName = "AudioSwitcherDevices")]
        public IDiffSpread<EnumEntry> DeviceSelection;

        [Input("Set", IsBang = true)]
        public IDiffSpread<bool> SetVolume;

        //[Output("Volume Out")]
        //public ISpread<double> GetVolume;
        [Output("Loading Engine")]
        public ISpread<bool> Loading;

        private CoreAudioController _audioctrl;
        private readonly Dictionary<string, CoreAudioDevice> _devassoc = new Dictionary<string, CoreAudioDevice>();
        //private Stopwatch _timer = new Stopwatch();

        private bool _volumeChanged;
        private bool _devicesChanged;

        public void Evaluate(int SpreadMax)
        {
            if(_audioctrl == null) return;
            if (_devicesChanged)
            {
                var entries = _devassoc.Keys.Concat(Enumerable.Repeat("Default", 1)).ToArray();
                EnumManager.UpdateEnum("AudioSwitcherDevices", "Default", entries);
                //GetVolume.SliceCount = SpreadMax;
            }

            for (int i = 0; i < SpreadMax; i++)
            {
                if(!SetVolume[i]) continue;
                var device = _audioctrl.DefaultPlaybackDevice;
                if (_devassoc.ContainsKey(DeviceSelection[i].Name))
                    device = _devassoc[DeviceSelection[i].Name];

                device.SetVolumeAsync(Volume[i] * 100.0);
                //_timer.Restart();
            }
            
            //GetVolume.SliceCount = SpreadMax;
            //for (int i = 0; i < SpreadMax; i++)
            //{
            //    var device = _audioctrl.DefaultPlaybackDevice;
            //    if (_devassoc.ContainsKey(DeviceSelection[i].Name))
            //        device = _devassoc[DeviceSelection[i].Name];
            //    GetVolume[i] = device.Volume / 100.0;
            //}
            _devicesChanged = false;
            _volumeChanged = false;
        }

        public void OnImportsSatisfied()
        {
            Loading[0] = true;
            //_timer.Start();
            Task.Run(() =>
            {
                _audioctrl = new CoreAudioController();
                _audioctrl.AudioDeviceChanged.Subscribe(args =>
                {
                    switch (args.ChangedType)
                    {
                        case DeviceChangedType.DefaultChanged:
                            break;
                        case DeviceChangedType.DeviceAdded:
                            GetDevices();
                            break;
                        case DeviceChangedType.DeviceRemoved:
                            GetDevices();
                            break;
                        case DeviceChangedType.PropertyChanged:
                            break;
                        case DeviceChangedType.StateChanged:
                            break;
                        case DeviceChangedType.MuteChanged:
                            break;
                        case DeviceChangedType.VolumeChanged:
                            _volumeChanged = true;
                            break;
                        case DeviceChangedType.PeakValueChanged:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
                GetDevices();
            });
        }

        public void GetDevices()
        {
            var devtask = _audioctrl.GetPlaybackDevicesAsync(DeviceState.All);
            devtask.ContinueWith(task =>
            {
                var devices = task.Result;
                _devassoc.Clear();
                foreach (var device in devices)
                {
                    _devassoc.UpdateGeneric(device.FullName, device);
                }
                _devicesChanged = true;
                Loading[0] = false;
            });
        }
    }
}
