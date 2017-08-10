using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
	public class Frame
	{
		public Frame(DateTime time, Originators originator)
		{
			TimeStamp = time;
			Originator = originator;
		}

		public DateTime TimeStamp { get; set; }
		public Originators Originator { get; set; }

		int FrameIndex = 0;
		int FramedataIndex = 0;

		public bool FrameAcknowledged { get; set; } = false;
		public int FrameSource { get; set; } = 0;

		private int FrameDestination = 0;

		public int GetFrameDestination()
		{
			return FrameDestination;
		}

		public void SetFrameDestination(int value)
		{
			FrameDestination = value;
		}

		public int FrameCommand { get; set; } = 0;
		public int FrameStatusCode { get; set; } = 0;
		public int FrameTransactionNumber { get; set; } = 0;
		public int FrameFunction { get; set; } = 0;
		public int FrameAddress { get; set; } = 0;
		public int DataSize { get; set; } = 0;
		public byte[] FrameData = new byte[256];
		public int FrameCrcChecksum { get; set; } = 0;

		public bool AddToFrame(byte _byte)
		{
			switch (FrameIndex++)
			{
				case 0:
					FrameSource = _byte;
					return true;

				case 1:
					FrameDestination = _byte;
					return true;

				case 2:
					FrameCommand = _byte;
					return true;

				case 3:
					FrameStatusCode = _byte;
					return true;

				case 4:
					FrameTransactionNumber = _byte;
					return true;

				case 5:
					FrameTransactionNumber = _byte << 8 | FrameTransactionNumber;
					return true;

				case 6:
					FrameFunction = _byte;
					return true;

				case 7:
					FrameAddress = _byte;
					return true;

				case 8:
					FrameAddress = FrameAddress << 8 | _byte;
					return true;

				case 9:
					DataSize = _byte;
					return true;
				
				default:
					FrameData[FramedataIndex++] = _byte;
					return true;
			}
		}

		public bool AddCrcToFrame(int _byte)
		{
			FrameCrcChecksum = FrameAddress << 8 | _byte;
			return true;
		}

		public override string ToString()
		{
			return String.Format("{1:mm:ss} {0}({6}) Command({4})  Status {5}  " +
				"\nAddres - {7}" +
				"\nData Size - {8}  Data - {9}" +
				"\n----ACK - {10} \n\n"
				, Originator, TimeStamp,
				FrameSource, FrameDestination, FrameStatusCode, FrameCommand.ToString("X2")
				, FrameTransactionNumber, FrameAddress, DataSize, FramedataIndex, FrameAcknowledged);
		}
	}
}
