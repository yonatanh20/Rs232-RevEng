using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    public enum FunctionTypes
    {
        ProtectedRead = 0xA2,
        ProtectedWrite = 0xAA
    }
    public enum FileTypes
    {
        Reserved = 0x80,
        Reserved1 = 0x81,
        Reserved2 = 0x82,
        Reserved3 = 0x83,
        Status = 0x84,
        Bit = 0x85,
        Timer = 0x86,
        Counter = 0x87,
        Control = 0x88,
        Integer = 0x89,
        FloatingPoint = 0x8A,
        OutputLogical = 0x8B,
        InputLogical = 0x8C,
        String = 0x8D,
        ASCII = 0x8E,
        BCD = 0x8F
    }

    public class Df1BaseCmd
    {
        public Frame CommandFrame { get; private set; }
        public Frame ReplyFrame { get; private set; }

        public FunctionTypes FunctionType { get; protected set; }

        public Df1BaseCmd(Frame commandFrame, Frame replyFrame)
        {
            CommandFrame = commandFrame;
            ReplyFrame = replyFrame;
        }


        public override string ToString()
        {
            return String.Format("UnknownCommand - {0}.\n", CommandFrame.FunctionCode.ToString("X2"));
            
        }

        public int TakeVarInt2(byte[] data, ref int offset)
        {
            int val = data[offset++];

            if (val == 0xFF)
            {
                val = BitConverter.ToInt32(data, offset);
                offset += 2;
            }

            return val;
        }
    }

    public class ProtectedReadCmd : Df1BaseCmd
    {
        public int BytesRequested { get; set; } = 0;
        public int FileNumber { get; set; } = 0;
        public FileTypes FileType { get; set; } = 0;
        public int ElementNumber { get; set; } = 0;
        public int SubElementNumber { get; set; } = 0;
        public byte[] Data { get; private set; }

        public ProtectedReadCmd(Frame commandFrame, Frame replyFrame) : base(commandFrame, replyFrame)
        {
            int idx = 0;
            FunctionType = FunctionTypes.ProtectedRead;
            BytesRequested = CommandFrame.Data[idx++];

            FileNumber = TakeVarInt2(CommandFrame.Data, ref idx);
            FileType = (FileTypes)CommandFrame.Data[idx++];
            ElementNumber = TakeVarInt2(CommandFrame.Data, ref idx);
            SubElementNumber = TakeVarInt2(CommandFrame.Data, ref idx);           

            Data = new ArraySegment<byte>(CommandFrame.Data, idx, BytesRequested).ToArray();
        }

        public override string ToString()
        {
            return String.Format("{0:mm:ss} ProtectedRead(File={1},Element={2},SubElement={3},Type={4})\n" +
                "<-- {5:00} {6}\n",
                CommandFrame.TimeStamp, FileNumber, ElementNumber, SubElementNumber, FileType,
                Data.Length, ReplyFrame.ArrayToString(Data, Data.Length));
        }
    }

    public class ProtectedWriteCmd : Df1BaseCmd
    {
        public int BytesRequested { get; private set; } = 0;
        public int FileNumber { get; private set; } = 0;
        public FileTypes FileType { get; private set; } = 0;
        public int ElementNumber { get; private set; } = 0;
        public int SubElementNumber { get; private set; } = 0;
        public byte[] Data { get; private set; }

        public ProtectedWriteCmd(Frame commandFrame, Frame replyFrame) : base(commandFrame, replyFrame)
        {
            int idx = 0;

            FunctionType = FunctionTypes.ProtectedWrite;

            BytesRequested = CommandFrame.Data[idx++];

            FileNumber = TakeVarInt2(CommandFrame.Data, ref idx);
            FileType = (FileTypes)CommandFrame.Data[idx++];
            ElementNumber = TakeVarInt2(CommandFrame.Data, ref idx);
            SubElementNumber = TakeVarInt2(CommandFrame.Data, ref idx);

            Data = new ArraySegment<byte>(CommandFrame.Data, idx, BytesRequested).ToArray();
        }

        public override string ToString()
        {
            return String.Format("{0:mm:ss} ProtectedWrite(File={1},Element={2},SubElement={3},Type={4})\n" +
                "==> {5:00} {6}\n",
                CommandFrame.TimeStamp, FileNumber, ElementNumber, SubElementNumber, FileType,
                Data.Length, ReplyFrame.ArrayToString(Data, Data.Length));
        }
    }
}
