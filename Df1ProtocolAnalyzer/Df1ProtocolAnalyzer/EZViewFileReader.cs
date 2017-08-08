using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Df1ProtocolAnalyzer
{
    public enum Originators
    {
        DCE,
        DTE,
        Error
    }

    public enum Df1TransmissionSymbols
    {
        STX = 0x02,
        ETX = 0x03,
        ENQ = 0x05,
        ACK = 0x06,
        NAK = 0x0F,
        DLE = 0x10
    };

    public class EZViewFileReader
    {
        const int EOF = -1;
        const int EZV_DATA_START = 66;

        private FileStream _fileStream;
        public byte[] _data = new byte[8];
        DateTime _baseDate;

        public EZViewFileReader(string filepath)
        {
            _fileStream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            byte[] header = new byte[EZV_DATA_START];
            if (_fileStream.Read(header, 0, header.Length) < header.Length)
                throw new Exception("Data file does not contain a valid capture");
            var fudge = 17*365*24*3600;
            _baseDate = new DateTime(2001, 1, 1, 0, 0, 0).AddSeconds(BitConverter.ToInt32(header, 38)+fudge);
        }

        public int Read()
        {
            if (_fileStream.Read(_data, 0, _data.Length) < _data.Length)
                return EOF;

            return _data[7];
        }

        public byte DataByte
        {
            get
            {
                return _data[7];
            }
        }

        public byte PeekByte
        {
            get
            {
                return _data[7];
            }
        }

        public DateTime Timestamp
        {
            get
            {
                var epochSecs = BitConverter.ToInt32(_data, 2);
                return _baseDate.AddMilliseconds(Convert.ToDouble(epochSecs));
            }
        }

        public int TimeOffset
        {
            get
            {
                return BitConverter.ToInt32(_data, 2);
            }
        }

        public Originators Originator
        {
            get
            {
                switch (_data[6])
                {
                    case 1:
                        return Originators.DTE;

                    case 2:
                        return Originators.DCE;

                    default:
                        return Originators.Error;
                }
            }
        }
        
    }
}
