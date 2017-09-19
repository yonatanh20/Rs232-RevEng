using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    public struct ByteDef
    {
        public byte DataByte;
        public DateTime Timestamp;
        public Originators Originator;

        public ByteDef(byte dataByte, DateTime timestamp , Originators originator)
        {
            DataByte = dataByte;
            Timestamp = timestamp;
            Originator = originator;
        }
    }
    public interface IRS232Reader
    {
        IEnumerable<ByteDef> Read();

        byte DataByte { get; }

        DateTime Timestamp { get; }

        Originators Originator { get; }
    }
}