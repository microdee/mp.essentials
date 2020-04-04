using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.devices.Hid
{
    public class HidDeviceWrap : IDisposable
    {
        private byte[] _inputBytes;
        private byte[] _rawInputBytes;
        private byte[] _outputBytes;

        public HidDevice Device { get; set; }

        public ReportDescriptor Descriptor { get; private set; }

        public HidDeviceInputReceiver Receiver { get; private set; }

        public int ChangedCounter { get; private set; }

        [VvvvIgnore]
        public HidStream Stream { get; private set; }

        public int InputLength { get; private set; }
        public int MaxInputReportLength { get; private set; }
        public int InputReportCount { get; private set; }

        //[VvvvNoBinSizing]
        public Dictionary<int, int> InputReportOffsets { get; } = new Dictionary<int, int>();

        public int OutputLength { get; private set; }

        public string DevicePath { get; private set; }

        private bool _ready;

        public bool Opened { get; private set; }
        public bool Opening { get; private set; }
        public Exception Error { get; private set; }

        private void Change()
        {
            ChangedCounter++;
            HidChange.GlobalChangeCounter++;
        }

        public HidDeviceWrap Initialize()
        {
            Error = null;
            Opened = false;
            _ready = false;

            DevicePath = Device.DevicePath;
            Change();

            Task.Run(() =>
            {
                Opening = true;
                try
                {
                    //Stream = Device.Open();

                    Descriptor = Device.GetReportDescriptor();

                    MaxInputReportLength = Device.GetMaxInputReportLength();
                    InputReportCount = 0;
                    InputReportOffsets.Clear();
                    foreach (var report in Descriptor.InputReports)
                    {
                        InputReportOffsets.Add(report.ReportID, InputReportCount * MaxInputReportLength);
                        InputReportCount++;
                    }
                    InputLength = MaxInputReportLength * InputReportCount;
                    OutputLength = Device.GetMaxOutputReportLength();

                    _inputBytes = new byte[InputLength];
                    _rawInputBytes = new byte[MaxInputReportLength];
                    _outputBytes = new byte[OutputLength];

                    Receiver = Descriptor.CreateHidDeviceInputReceiver();

                    var options = new OpenConfiguration();
                    options.SetOption(OpenOption.Exclusive, false);
                    options.SetOption(OpenOption.Transient, false);
                    options.SetOption(OpenOption.Interruptible, false);
                    options.SetOption(OpenOption.Priority, OpenPriority.High);

                    Stream = Device.Open(options);

                    Receiver.Start(Stream);
                    Receiver.Received += (sender, args) =>
                    {
                        Opened = true;
                        _ready = true;
                        Change();
                    };

                    Opened = true;
                }
                catch (Exception e)
                {
                    Opened = false;
                    Error = e;
                }
                Opening = false;
                Change();
                _ready = Opened;
            });
            return this;
        }

        public HidDeviceWrap Open()
        {
            if (Opened) return this;
            Error = null;
            Opened = false;
            _ready = false;
            Change();
            Task.Run(() =>
            {
                Opening = true;
                try
                {
                    Stream = Device.Open();
                    Opened = _ready = true;
                }
                catch (Exception e)
                {
                    Opened = _ready = false;
                    Error = e;
                }
                Opening = false;
                Change();
            });
            return this;
        }

        public HidDeviceWrap Close()
        {
            if (!Opened) return this;
            try
            {
                Stream.Close();
            }
            catch (Exception e)
            {
                Error = e;
            }
            Opened = false;
            return this;
        }

        public HidDeviceWrap Read(Stream input, int reportId = -1)
        {
            if (!_ready || !Opened || input == null) return this;
            while (Receiver.TryRead(_rawInputBytes, 0, out var report))
            {
                int offset = InputReportOffsets[report.ReportID];
                Buffer.BlockCopy(_rawInputBytes, 0, _inputBytes, offset, report.Length);
            }

            if (reportId < 0)
            {
                input.SetLength(InputLength);
                input.Position = 0;
                input.Write(_inputBytes, 0, InputLength);
            }
            else if (InputReportOffsets.ContainsKey(reportId))
            {
                input.SetLength(MaxInputReportLength);
                input.Position = 0;
                input.Write(_inputBytes, InputReportOffsets[reportId], MaxInputReportLength);
            }
            else
            {
                input.SetLength(0);
            }
            return this;
        }

        public HidDeviceWrap Send(Stream output)
        {
            if (!_ready || !Opened) return this;
            output.Position = 0;
            output.Read(_outputBytes, 0, (int)Math.Min(output.Length, OutputLength));
            output.Position = 0;
            try
            {
                Stream.Write(_outputBytes);
            }
            catch (Exception e)
            {
                Error = e;
            }
            Change();
            return this;
        }

        public void Dispose()
        {
            _ready = false;
            Opened = false;
            Stream?.Close();
            Stream?.Dispose();
        }
    }

    [PluginInfo(
        Name = "Device",
        Author = "microdee",
        Category = "HID"
    )]
    public class HidDeviceWrapSplitNode : ObjectSplitNode<HidDeviceWrap>
    {
        [Input("Read", Order = 100)]
        protected IDiffSpread<bool> ReadIn;

        [Input(
            "Filter Report ID",
            Order = 101,
            BinOrder = 102,
            DefaultValue = -1
        )]
        protected IDiffSpread<ISpread<int>> FilterReportIdIn;

        [Input(
            "Output Report",
            Order = 103,
            BinOrder = 104
        )]
        protected ISpread<ISpread<Stream>> OutputReportIn;

        [Input(
            "Send",
            IsBang = true,
            Order = 105,
            BinOrder = 106
        )]
        protected ISpread<ISpread<bool>> SendIn;

        [Output(
            "Input Report",
            Order = -10,
            BinOrder = -9
        )]
        protected ISpread<ISpread<Stream>> InputReportOut;

        public override void OnEvaluateBegin()
        {
            base.OnEvaluateEnd();
            for (int i = 0; i < FInput.SliceCount; i++)
            {
                if (FInput[i] == null) continue;
                var device = FInput[i];
                for (int j = 0; j < SpreadUtils.SpreadMax(OutputReportIn[i], SendIn[i]); j++)
                {
                    if (SendIn[i][j])
                    {
                        device.Send(OutputReportIn[i][j]);
                    }
                }
            }
        }

        public override void OnEvaluateEnd()
        {
            base.OnEvaluateEnd();
            if (FInput.IsChanged || SpreadUtils.AnyChanged(ReadIn, FilterReportIdIn))
            {
                InputReportOut.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if (FInput[i] == null || !ReadIn[i])
                    {
                        InputReportOut[i].ResizeAndDispose(0, () => new MemoryStream());
                        continue;
                    }

                    var insprmax = FilterReportIdIn[i].SliceCount;
                    InputReportOut[i].ResizeAndDispose(insprmax, () => new MemoryStream());
                    var device = FInput[i];

                    for (int j = 0; j < insprmax; j++)
                    {
                        if (InputReportOut[i][j] == null) InputReportOut[i][j] = new MemoryStream();
                        device.Read(InputReportOut[i][j], FilterReportIdIn[i][j]);
                    }
                }
            }
        }
    }

    [PluginInfo(
        Name = "HidDeviceInputReceiver",
        Author = "microdee",
        Category = "HID"
    )]
    public class HidDeviceInputReceiverSplitNode : ObjectSplitNode<HidDeviceInputReceiver> { }

    [PluginInfo(
        Name = "Report",
        Author = "microdee",
        Category = "HID"
    )]
    public class ReportSplitNode : ObjectSplitNode<Report> { }

    [PluginInfo(
        Name = "DeviceItem",
        Author = "microdee",
        Category = "HID"
    )]
    public class DeviceItemSplitNode : ObjectSplitNode<DeviceItem> { }

    [PluginInfo(
        Name = "ReportDescriptor",
        Author = "microdee",
        Category = "HID"
    )]
    public class ReportDescriptorSplitNode : ObjectSplitNode<ReportDescriptor> { }

    public class HidDeviceCollection : IDisposable
    {
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public int? ReleaseNumber { get; set; }
        public Spread<HidDeviceWrap> Devices { get; } = new Spread<HidDeviceWrap>();

        public Exception Error { get; private set; }

        public HidDeviceCollection GetDevice()
        {
            Error = null;
            try
            {
                var candidates = DeviceList.Local.GetHidDevices(VendorId, ProductId, ReleaseNumber);
                //if(Interfaces == null) Interfaces = new Spread<HidInterfaceWrap>();
                if (Devices.SliceCount != 0)
                {
                    foreach (var wrap in Devices)
                    {
                        wrap.Dispose();
                    }
                }
                Devices.SliceCount = 0;
                foreach (var candidate in candidates)
                {
                    try
                    {
                        VendorId = candidate.VendorID;
                        ProductId = candidate.ProductID;
                        ReleaseNumber = candidate.ReleaseNumberBcd;
                        Devices.Add(new HidDeviceWrap
                        {
                            Device = candidate
                        }.Initialize());
                    }
                    catch (Exception e)
                    {
                        Error = e;
                    }
                }
            }
            catch (Exception e)
            {
                Error = e;
            }
            return this;
        }

        public HidDeviceCollection Close()
        {
            if (Devices == null) return this;
            foreach (var wrap in Devices)
            {
                wrap.Close();
            }
            return this;
        }

        public HidDeviceCollection Open()
        {
            if (Devices == null) return this;
            try
            {
                foreach (var wrap in Devices)
                {
                    wrap.Open();
                }
            }
            catch (Exception e)
            {
                Error = e;
            }
            return this;
        }

        public void Dispose()
        {
            if (Devices == null) return;
            foreach (var wrap in Devices)
            {
                wrap.Dispose();
            }
        }
    }

    [PluginInfo(
        Name = "DeviceCollection",
        Category = "HID",
        Author = "microdee",
        Help = "Splits a connected composite device which can report multiple HID devices or a collection of devices"
    )]
    public class HidDeviceCollectionSplitNode : ObjectSplitNode<HidDeviceCollection>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            if (original == typeof(int?)) return typeof(int);
            return base.TransformType(original, member);
        }
    }

    [PluginInfo(
        Name = "HidDevice",
        Category = "HID",
        Author = "microdee",
        Help = "Gets additional information about an HID device"
    )]
    public class HidDeviceSplitNode : ObjectSplitNode<HidDevice>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            if (original == typeof(int?)) return typeof(int);
            return base.TransformType(original, member);
        }
    }
}
