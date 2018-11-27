using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using md.stdl.StreamUtils;
using mp.essentials.Nodes.Excel;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.network
{
    [PluginInfo(
        Name = "ResolutionPrefixTokenizer2DDelegate",
        Category = "Tokenizer",
        Author = "microdee"
    )]
    public class ResolutionPrefixTokenizer2DDelegateNode : TokenizerDelegateAbstractNode<ResolutionPrefixTokenizer2D> { }

    public abstract class TokenizerDelegateAbstractNode<T> : ClassObjectJoinNode<T>, IPluginEvaluate
        where T : IByteStreamTokenizer, new()
    {
        [Output("Output")]
        public ISpread<IByteStreamTokenizer> Output;

        private T _tokenizer;

        public void Evaluate(int SpreadMax)
        {
            if (_tokenizer == null) _tokenizer = CreateObject();
            if (Pd.InputChanged)
            {
                FillObject(_tokenizer, 0);
                Output[0] = _tokenizer;
            }
        }
    }

    [PluginInfo(
        Name = "TcpClient",
        Category = "Network",
        Version = "Raw",
        Author = "microdee",
        Tags = "advanced, async, clr",
        AutoEvaluate = true
    )]
    public class TcpAdvancedNode : IPluginEvaluate, IDisposable
    {
        private object _lock = new object();
        private TcpClient _tcp;
        private Task<Exception> _tcpTask;
        private MemoryStream _delimitedBuf;
        
        private bool _cancel;
        private bool _tokenizedDataAvailable;

        private byte[] _sendBuffer;
        private byte[] _recBuf0;
        private byte[] _recBuf1;
        private bool _recbufSwitch;
        private int _recBufPos;
        private bool _onData;

        [Input("Output Data")]
        public ISpread<Stream> DataIn;
        [Input("Send", IsBang = true)]
        public ISpread<bool> SendIn;

        [Input("Remote Host", StringType = StringType.IP)]
        public IDiffSpread<string> IpIn;
        [Input("Remote Port", DefaultValue = 4444)]
        public IDiffSpread<int> PortIn;

        [Input("Receive Buffer Size", DefaultValue = 131072)]
        public IDiffSpread<int> RecBufSizeIn;
        [Input("Receive Timeout Seconds", DefaultValue = 10.0)]
        public IDiffSpread<double> RecTimeoutSecsIn;

        [Input("Send Buffer Size", DefaultValue = 32768)]
        public IDiffSpread<int> SendBufSizeIn;
        [Input("Send Timeout Seconds", DefaultValue = 10.0)]
        public IDiffSpread<double> SendTimeoutSecsIn;

        [Input("Tokenizer")]
        public Pin<IByteStreamTokenizer> TokenizerIn;
        [Input("Use Tokenizer")]
        public ISpread<bool> UseTokenizerIn;

        [Input("Reconnect", IsBang = true)]
        public ISpread<bool> ReconnectIn;
        [Input("Close", IsBang = true)]
        public ISpread<bool> CloseIn;

        [Output("Received Data")]
        public ISpread<Stream> DataOut;
        [Output("Accumulated Bytes")]
        public ISpread<int> AccumulatedBytesOut;

        [Output("Connecting")]
        public ISpread<bool> ConnectingOut;
        [Output("OnData", IsBang = true)]
        public ISpread<bool> OnDataOut;
        [Output("Error")]
        public ISpread<bool> ErrorOut;
        [Output("Connected")]
        public ISpread<bool> ConnectedOut;
        [Output("TCP Out")]
        public ISpread<TcpClient> TcpOut;
        [Output("Exception Out")]
        public ISpread<Exception> ExceptionOut;

        private bool _init = true;

        private Exception TcpTaskBody()
        {
            try
            {
                ConnectingOut[0] = true;
                _tcp.Connect(IpIn[0], PortIn[0]);
                ConnectingOut[0] = false;
            }
            catch (Exception e)
            {
                ConnectedOut[0] = false;
                ErrorOut[0] = true;
                ExceptionOut[0] = e;
                return e;
            }

            var reader = _tcp.GetStream();

            ConnectedOut[0] = true;
            while (true)
            {
                if (_cancel)
                {
                    ConnectedOut[0] = false;
                    ErrorOut[0] = false;
                    return null;
                }
                try
                {
                    byte[] buf;
                    lock (_lock)
                    {
                        buf = _recBuf0;
                    }
                    if (_recBufPos >= buf.Length - 1) continue;
                    var l = Math.Max(1, buf.Length - _recBufPos - 1);
                    var r = reader.Read(buf, _recBufPos, l);
                    lock (_lock)
                    {
                        _recBufPos += r;
                        _onData = true;
                    }
                }
                catch (Exception e)
                {
                    ConnectedOut[0] = false;
                    ErrorOut[0] = true;
                    ExceptionOut[0] = e;
                    return e;
                }
            }
            return null;
        }

        private void CloseConnection()
        {
            if (_tcp != null && _tcpTask != null)
            {
                if (!_tcpTask.IsCompleted && !_tcpTask.IsFaulted)
                {
                    _cancel = true;
                    _tcpTask.Wait(1000);
                }
                _tcp.Close();
                _tcp.Dispose();
            }
        }

        private void OpenConnection()
        {
            _tcp = new TcpClient
            {
                ReceiveBufferSize = RecBufSizeIn[0],
                ReceiveTimeout = (int)(RecTimeoutSecsIn[0] * 1000.0),
                SendBufferSize = SendBufSizeIn[0],
                SendTimeout = (int)(SendTimeoutSecsIn[0] * 1000.0)
            };
            _sendBuffer = new byte[SendBufSizeIn[0]];
            _recBuf0 = new byte[RecBufSizeIn[0]];
            _recBuf1 = new byte[RecBufSizeIn[0]];
            _tcpTask = new Task<Exception>(TcpTaskBody);
            _tcpTask.Start();
        }

        public void Evaluate(int SpreadMax)
        {
            if (
                _init ||
                SpreadUtils.AnyChanged(IpIn, PortIn, RecBufSizeIn, RecTimeoutSecsIn, SendBufSizeIn, SendTimeoutSecsIn) ||
                ReconnectIn[0]
                )
            {
                CloseConnection();
                OpenConnection();
                _init = false;
            }

            if (CloseIn[0]) CloseConnection();
            
            TcpOut[0] = _tcp;
            
            if(_tcpTask.IsCompleted || _tcpTask.IsFaulted)
            {
                CloseConnection();
                ExceptionOut[0] = _tcpTask.Exception;
                //if (_tcpTask.Result != null || _tcpTask.IsFaulted)
                //{
                //    OpenConnection();
                //}
                return;
            }

            if(!ConnectedOut[0]) return;

            if (SendIn[0])
            {
                var l = (int)Math.Min(DataIn[0].Length, SendBufSizeIn[0]);
                DataIn[0].Position = 0;
                DataIn[0].Read(_sendBuffer, 0, l);
                _tcp.Client.BeginSend(_sendBuffer, 0, l, SocketFlags.None, ar => { }, null);
            }
            
            int length;
            byte[] buf;

            lock (_lock)
            {
                OnDataOut[0] = _onData;
                length = _recBufPos;
                //buf = _recbufSwitch ? _recBuf1 : _recBuf0;
                buf = _recBuf0;
                _recBuf0 = new byte[SendBufSizeIn[0]];
                _recBufPos = 0;
                _onData = false;
            }

            DataOut.SliceCount = 1;
            if(DataOut[0] == null) DataOut[0] = new MemoryStream(RecBufSizeIn[0]);
            if (UseTokenizerIn[0])
            {
                if(_delimitedBuf == null) _delimitedBuf = new MemoryStream(RecBufSizeIn[0]);

                if (OnDataOut[0])
                {
                    for (int i = 0; i < length; i++)
                    {
                        var tknr = TokenizerIn[0];
                        tknr.CheckByte(buf[i]);
                        if (tknr.DataStart || tknr.Invalidate)
                        {
                            _delimitedBuf.Position = 0;
                            _delimitedBuf.SetLength(0);
                        }

                        if (tknr.ByteValid)
                        {
                            _delimitedBuf.WriteByte(buf[i]);
                        }

                        if (tknr.DataComplete)
                        {
                            _delimitedBuf.Position = 0;
                            DataOut[0].Position = 0;
                            DataOut[0].SetLength(_delimitedBuf.Length);
                            _delimitedBuf.CopyTo(DataOut[0]);
                        }
                    }
                    AccumulatedBytesOut[0] = (int)_delimitedBuf.Length;
                }
            }
            else
            {
                if (OnDataOut[0])
                {
                    AccumulatedBytesOut[0] = length;
                    DataOut[0].Position = 0;
                    DataOut[0].SetLength(length);
                    DataOut[0].Write(buf, 0, length);
                }
                else
                {
                    DataOut[0].Position = 0;
                    DataOut[0].SetLength(0);
                }
            }
        }

        public void Dispose()
        {
            CloseConnection();
        }
    }
}
