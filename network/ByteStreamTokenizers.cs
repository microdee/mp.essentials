using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.StreamUtils;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.network
{
    public interface IByteStreamTokenizer
    {
        bool ByteValid { get; }
        bool DataStart { get; }
        bool DataComplete { get; }
        bool Invalidate { get; }
        void CheckByte(byte input);
    }

    public class LengthPrefixTokenizer : IByteStreamTokenizer
    {
        protected Stream LengthStream;

        protected bool InsideHeader;
        protected bool InsidePrefix;
        protected bool InsideData;
        protected bool InsideLength;

        protected int BytePos;
        protected int HeaderBytePos;
        protected int DataBytePos;

        protected int DataLength;
        protected LengthFormat _lengthType = LengthFormat.Int32;

        public enum LengthFormat
        {
            Byte = 1,
            UInt16 = 2,
            UInt32 = 4,
            UInt64 = 8,
            Int16 = 12,
            Int32 = 14,
            Int64 = 18
        }

        protected int GetLengthSize()
        {
            if ((int)_lengthType > 10)
            {
                return (int)_lengthType - 10;
            }
            else
            {
                return (int)_lengthType;
            }
        }

        public bool ByteValid { get; private set; }
        public bool DataStart { get; private set; }
        public bool DataComplete { get; private set; }
        public bool Invalidate { get; private set; }

        public Stream Header { get; set; }
        public int LengthOffset { get; set; }
        public int DataOffset { get; set; }

        public virtual LengthFormat LengthType
        {
            get => _lengthType;
            set
            {
                _lengthType = value;
                LengthStream = new MemoryStream(GetLengthSize());
            }
        }

        public LengthPrefixTokenizer()
        {
            LengthStream = new MemoryStream(GetLengthSize());
        }

        protected virtual int GetLengthScalar(Stream input)
        {
            switch (LengthType)
            {
                case LengthFormat.Byte:
                    return input.ReadByte();
                case LengthFormat.UInt16:
                    return input.ReadUshort();
                case LengthFormat.UInt32:
                    return (int)input.ReadUint();
                case LengthFormat.UInt64:
                    return (int)input.ReadUlong();
                case LengthFormat.Int16:
                    return input.ReadShort();
                case LengthFormat.Int32:
                    return input.ReadInt();
                case LengthFormat.Int64:
                    return (int)input.ReadLong();
                default:
                    return 0;
            }
        }

        protected virtual void CheckLength(byte input)
        {
            InsideLength = BytePos >= LengthOffset && BytePos < (LengthOffset + GetLengthSize());
            if (InsideLength)
            {
                LengthStream.Position = BytePos - LengthOffset;
                LengthStream.WriteByte(input);
            }

            if (BytePos == LengthOffset + GetLengthSize() - 1)
            {
                LengthStream.Position = 0;
                DataLength = GetLengthScalar(LengthStream);
            }
        }

        public void CheckByte(byte input)
        {
            Invalidate = false;
            DataComplete = false;
            DataStart = false;
            ByteValid = false;

            if (InsideData)
            {
                var fullLength = DataLength + DataOffset;
                if (BytePos < fullLength)
                {
                    ByteValid = true;
                    if (BytePos == fullLength - 1)
                    {
                        DataComplete = true;

                        InsideHeader = false;
                        InsidePrefix = false;
                        InsideData = false;

                        BytePos = 0;
                        HeaderBytePos = 0;
                        DataBytePos = 0;
                        return;
                    }
                }
                BytePos++;
            }

            if (InsidePrefix)
            {
                CheckLength(input);

                if (BytePos >= DataOffset)
                {
                    InsideData = true;
                    InsidePrefix = false;
                    DataBytePos = 0;
                }

                ByteValid = true;
                BytePos++;
            }

            if (InsideHeader)
            {
                var hb = (byte)Header.ReadByte();
                if (hb != input)
                {
                    ByteValid = false;
                    InsideHeader = false;
                    InsidePrefix = false;
                    InsideData = false;

                    Invalidate = true;
                    BytePos = 0;
                    HeaderBytePos = 0;
                    DataBytePos = 0;
                    return;
                }
                else
                {
                    ByteValid = true;
                    HeaderBytePos++;
                    BytePos++;
                }

                if (Header.Position >= Header.Length)
                {
                    InsideHeader = false;
                    InsidePrefix = true;
                }
            }

            if (!InsideHeader && !InsidePrefix && !InsideData)
            {
                Header.Position = 0;
                var hstart = (byte)Header.ReadByte();
                if (hstart == input)
                {
                    DataStart = true;
                    ByteValid = true;
                    InsideHeader = true;
                    InsidePrefix = true;

                    BytePos = 1;
                    HeaderBytePos = 1;
                    DataBytePos = 1;
                    DataLength = 0;
                }
            }
        }
    }

    [PluginInfo(
        Name = "LengthPrefixTokenizerDelegate",
        Category = "Tokenizer",
        Author = "microdee"
    )]
    public class LengthPrefixTokenizerDelegateNode : TokenizerDelegateAbstractNode<LengthPrefixTokenizer> { }

    public class ResolutionPrefixTokenizer2D : LengthPrefixTokenizer
    {
        //private static readonly int[] _lookup = new[] {1, 0, 3, 2};
        public int PixelSizeCoeff { get; set; } = 12;

        public ResolutionPrefixTokenizer2D()
        {
            LengthStream = new MemoryStream(GetLengthSize() * 2);
        }

        public override LengthFormat LengthType
        {
            get => _lengthType;
            set
            {
                _lengthType = value;
                LengthStream = new MemoryStream(GetLengthSize() * 2);
            }
        }

        protected override void CheckLength(byte input)
        {
            var lengthendpos = LengthOffset + GetLengthSize() * 2;
            InsideLength = BytePos >= LengthOffset && BytePos < lengthendpos;
            if (InsideLength)
            {
                var p = BytePos - LengthOffset;
                LengthStream.Position = p;
                LengthStream.WriteByte(input);
            }

            if (BytePos == lengthendpos - 1)
            {
                LengthStream.Position = 0;
                var x = GetLengthScalar(LengthStream);
                var y = GetLengthScalar(LengthStream);
                DataLength = x * y * PixelSizeCoeff;
            }
        }
    }

    [PluginInfo(
        Name = "Tokenizer",
        Category = "Raw",
        Version = "ByteDelegate",
        Author = "microdee"
    )]
    public class ByteTokenizerNode : IPluginEvaluate
    {
        private MemoryStream _delimitedBuf = new MemoryStream();

        [Input("Input")]
        public Pin<Stream> Input;
        [Input("Tokenizer")]
        public Pin<IByteStreamTokenizer> TokenizerIn;

        [Output("Output")]
        public ISpread<Stream> Output;

        public void Evaluate(int SpreadMax)
        {
            if (!Input.IsConnected || Input.SliceCount <= 0 || Input[0] == null) return;
            if (!TokenizerIn.IsConnected || TokenizerIn.SliceCount <= 0 || TokenizerIn[0] == null) return;
            Input[0].Position = 0;
            if(Output[0] == null) Output[0] = new MemoryStream();
            for (int i = 0; i < Input[0].Length; i++)
            {
                var tknr = TokenizerIn[0];
                var b = (byte)Input[0].ReadByte();
                tknr.CheckByte(b);
                if (tknr.DataStart || tknr.Invalidate)
                {
                    _delimitedBuf.Position = 0;
                    _delimitedBuf.SetLength(0);
                }

                if (tknr.ByteValid)
                {
                    _delimitedBuf.WriteByte(b);
                }

                if (tknr.DataComplete)
                {
                    _delimitedBuf.Position = 0;
                    Output[0].Position = 0;
                    Output[0].SetLength(_delimitedBuf.Length);
                    _delimitedBuf.CopyTo(Output[0]);
                }
            }
        }
    }
}
