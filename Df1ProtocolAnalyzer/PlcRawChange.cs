using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{

    public class PlcRawChange
    {
        public byte[] OldData { get; private set; }
        public byte[] NewData { get; private set; }
        public Df1BaseCmd NewCommand { get; private set; }

        public int FileNumber { get; private set; } = 0;
        public FileTypes FileType { get; private set; } = 0;
        public int ElementNumber { get; private set; } = 0;
        public int SubElementNumber { get; private set; } = 0;
        public Type CommandType { get; private set; }

        public List<Tuple<int, bool>> ChangedBits { get; private set; } = new List<Tuple<int, bool>>();

        private bool BytesMatched { get; set; } = false;

        public PlcRawChange(byte[] oldData, Df1BaseCmd newCommand)
        {
            ProtectedReadCmd prNew;
            ProtectedWriteCmd pwNew;

            OldData = oldData;
            NewCommand = newCommand;

            if ((prNew = NewCommand as ProtectedReadCmd) != null)
            {
                FileNumber = prNew.FileNumber;
                FileType = prNew.FileType;
                ElementNumber = prNew.ElementNumber;
                SubElementNumber = prNew.SubElementNumber;

                CommandType = typeof(ProtectedReadCmd);
                NewData = prNew.Data;
                FindChangedBits(OldData, prNew.Data);
            }
            else if ((pwNew = NewCommand as ProtectedWriteCmd) != null)
            {
                FileNumber = pwNew.FileNumber;
                FileType = pwNew.FileType;
                ElementNumber = pwNew.ElementNumber;
                SubElementNumber = pwNew.SubElementNumber;

                CommandType = typeof(ProtectedWriteCmd);
                NewData = pwNew.Data;
                FindChangedBits(OldData, pwNew.Data);
            }
        }
        
        private void FindChangedBits(byte[] _old, byte[] _new)
        {
            int oldLength = _old.Length;
            int newLength = _new.Length;
            int oldVal, newVal , byteSize = 8;
            int minLength = oldLength > newLength ? newLength : oldLength;

            if (newLength == oldLength)
                BytesMatched = true;

            for (int i = 0; i < minLength; i++)
            {
                if ((oldVal = _old[i]) != (newVal = _new[i]))
                {
                    for (int j = 0, mask = 1; j < byteSize; j++, mask <<= 1)
                    {
                        if (((oldVal & mask) ^ (newVal & mask)) != 0)
                            ChangedBits.Add(new Tuple<int, bool>(i * 8 + j, (newVal & mask) != 0));
                    }
                }
            }
            

        }
        public override string ToString()
        {
            var commandtype = (CommandType == typeof(ProtectedWriteCmd)) ? "writen" : "read";
            StringBuilder changes = new StringBuilder($"({FileNumber:X2}-{ElementNumber:X2}-{FileType}) changed at {NewCommand.CommandFrame.TimeStamp:(mm:ss)} { commandtype}: \n");
            foreach (Tuple<int, bool> byteChanged in ChangedBits)
            {
                changes.Append($"[{byteChanged.Item1}] turned {(byteChanged.Item2?"on" : "off")} \n");
            }
            if (BytesMatched)
                return changes.Append(" --- Number of bytes matched ---\n").ToString();
            else
                return changes.Append(" --- Number of bytes  didn't match ---\n\n\n").ToString();

        }
    }
}
