using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    public enum CommandCodes
    {
        Code_0F = 0x0F
    }

    public enum FrameType
    {
        Command = 0x00,
        Reply = 0x40,
        Error
    }

    public enum HadReply
    {
        True,
        False,
        Error
    }

    public enum ExtSts
    {
        True = 0xF0,
        False,
        Error
    }


    public class Frame
	{

		public Frame(DateTime time, Originators originator)
		{
			TimeStamp = time;
			Originator = originator;
		}

		public DateTime TimeStamp { get; set; }
		public Originators Originator { get; set; }
        public bool FrameAcknowledged { get; set; } = false;
        public FrameType Type { get; set; } = FrameType.Error;
        
        
        int FrameIndex = 0;

		public int FramedataIndex { get; set; } = 0;
		public int Source { get; set; } = 0;
        public int Destination { get; set; } = 0;
		public int CommandCode { get; set; } = 0;
		public int StatusCode { get; set; } = 0;
		public int TransactionNumber { get; set; } = 0;
		public int FunctionCode { get; set; } = 0;
		public byte[] Data = new byte[256];
		public int Crc { get; set; } = 0;
        public bool HighPriority { get; set; } = false;

        public bool AddToFrame(byte _byte)
        {
            switch (FrameIndex++)
            {
                case 0:
                    Source = _byte;
                    return true;

                case 1:
                    Destination = _byte;
                    return true;

                case 2:

                    CommandCode = _byte & 0x0F;

                    HighPriority = (_byte & 0x20) != 0;

                    if ((_byte & 0x40) != 0)
                        Type = FrameType.Reply;
                    else
                        Type = FrameType.Command;

                    return true;

                case 3:
                    StatusCode = _byte;
                    return true;

                case 4:
                    TransactionNumber = _byte;
                    return true;

                case 5:
                    TransactionNumber = _byte << 8 | TransactionNumber;
                    return true;

                case 6:

                    if (Type != FrameType.Reply)
                        FunctionCode = _byte;
                    else
                        Data[FramedataIndex++] = _byte;
                    return true;

                default:
                    Data[FramedataIndex++] = _byte;
                    return true;
            }
        }

		public bool AddCrcToFrame(int _byte)
		{
			Crc = Crc << 8 | _byte;
			return true;
		}

        public override string ToString()
        {
            if (Type == FrameType.Command)
            {
                return String.Format("{1:mm:ss} {0}({6}) Command({4}) Function({10})  Status {5}  " +
                    "\nData Size - {7}  Data - {8}" +
                    "\n----ACK - {9} \n\n"
                    , Originator, TimeStamp,
                    Source, Destination, CommandCode.ToString("X2"), StatusCode
                    , TransactionNumber, FramedataIndex, ArrayToString(Data, FramedataIndex),
                    FrameAcknowledged, FunctionCode.ToString("X2"));
            }
            else
            {
                return String.Format("{1:mm:ss} {0}({6}) Command({4})  Status {5}  " +
                    "\nData Size - {7}  Data - {8}" +
                    "\n----ACK - {9} \n\n"
                    , Originator, TimeStamp,
                    Source, Destination, CommandCode.ToString("X2"), StatusCode
                    , TransactionNumber, FramedataIndex, ArrayToString(Data, FramedataIndex), FrameAcknowledged);
            }
        }

        public object ArrayToString(byte[] frameData, int length)
        {
            StringBuilder sb = new StringBuilder("[ ");

            for (int i = 0; i < length; i++)
                sb.AppendFormat("{0:X2} ", Data[i]);

            return sb.Append("]").ToString();
        }
   }

}

