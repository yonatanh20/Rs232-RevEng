using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    public class PlcControl
    {
        public string Name { get; private set; }
        public string Key { get; private set; }
        public int FileNumber { get; private set; } = 0;
        public FileTypes FileType { get; private set; } = 0;
        public int ElementNumber { get; private set; } = 0;
        public int SubElementNumber { get; private set; } = 0;
        public DateTime TimeStamp { get; private set; }

        public int Offset { get; private set; } = 0;
        public bool OnOff { get; private set; } = false;

        public int IntVal { get; private set; } = 0;


        public object State { get; set; }
        public string Text { get; set; }
        public Type CommandType { get; private set; }

        static Dictionary<string, string> s_NameLookup = new Dictionary<string, string>(256);
        public static DBUtil DbUtil { get; set; } = null;

        public PlcControl(PlcRawChange plc, Tuple<int, bool> chg)
        {
            FileNumber = plc.FileNumber;
            FileType = plc.FileType;
            ElementNumber = plc.ElementNumber;
            SubElementNumber = plc.SubElementNumber;
            Offset = chg.Item1;
            OnOff = chg.Item2;
            CommandType = plc.CommandType;
            IntVal = BitConverter.ToInt16(plc.NewData, (Offset/8) - (Offset/8) % 2 );
            TimeStamp = plc.TimeStamp;

            if (FileType == FileTypes.Integer)
                Key = $"{FileNumber:X2}-{FileType}-{ElementNumber:X2}-{SubElementNumber:X2}-{(Offset / 8) - (Offset / 8) % 2:X}";
            else
                Key = $"{FileNumber:X2}-{FileType}-{ElementNumber:X2}-{SubElementNumber:X2}-{Offset:X}";


            

            if (s_NameLookup.ContainsKey(Key))  // Name already known
            {
                Name = s_NameLookup[Key];
            }
            else if (null == DbUtil)    // No database was configured
            {
                Name = Key;
            }
            else    // Lookup in database (add if does not exist)
            {
                var c_Name = DbUtil.LookupControl(this);

                if (c_Name == null) // Need to insert a new control
                {
                    DbUtil.AddControl(this);
                    this.Name = Key;
                }
                else
                {
                    this.Name = c_Name;
                }

            }
            var h_Name = DbUtil.LookupHistory(this);

            if (h_Name == null) // Need to insert a new history documentation
            {
                DbUtil.AddHistory(this);
            }
            else
            {
                //This action was already documented.
            }
        }

        public override string ToString()
        {
            var commandtype = (CommandType == typeof(ProtectedWriteCmd)) ? "W" : "R";

            if (FileType == FileTypes.Bit || FileType == FileTypes.OutputLogical)
            {
                var onoff = OnOff ? "on" : "off";
                return $" {Name} was turned {onoff} : {commandtype}\n";
            }
            else if (FileType == FileTypes.Integer)
            {
                return $" {Name} value changes to {IntVal} : {commandtype}\n";
            }
            else
            {
                var onoff = OnOff ? "on" : "off";
                return $" {Name} was turned {onoff} : {commandtype}\n";
            }
        }
    }
}
