using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    public class PlcPlayback
    {
        Df1CommandReader _dcr;
        public PlcPlayback(Df1CommandReader dcr)
        {
            _dcr = dcr;
        }
        public IEnumerable<PlcRawChange> PlcChanges()
        {
            var map = new Dictionary<string, byte[]>();
            var key = "";
            var dcr = _dcr;
            foreach (var command in dcr.ReadCommand())
            {
                ProtectedReadCmd pr;
                ProtectedWriteCmd pw;
                PlcRawChange commandDiff;
                if ((pr = command as ProtectedReadCmd) != null)
                {
                    key = $"{pr.FileNumber:X2}-{pr.FileType}-{pr.ElementNumber:X2}-{pr.SubElementNumber:X2}";

                    if (!map.ContainsKey(key))  
                    {
                        map.Add(key,pr.Data);
                        continue;
                    }
                    if (!CompareArrays(map[key], pr.Data))
                    {
                        commandDiff = new PlcRawChange(map[key], pr);
                        map[key] = pr.Data;
                        yield return commandDiff;
                        continue;
                    }
                }
                if ((pw = command as ProtectedWriteCmd) != null)
                {
                    key = $"{pw.FileNumber:X2}-{pw.FileType}-{pw.ElementNumber:X2}-{pw.SubElementNumber:X2}";
                    if (!map.ContainsKey(key))
                    {
                        //The Write command is unique so we will save it regardless
                        var emptyarray = new byte[pw.Data.Length];
                        commandDiff = new PlcRawChange(emptyarray , pw);
                        yield return commandDiff;
                        map.Add(key, pw.Data);
                        continue;
                    }
                    if (!CompareArrays(map[key], pw.Data))
                    {

                        commandDiff = new PlcRawChange(map[key], pw);
                        map[key] = pw.Data;
                        yield return commandDiff;
                        continue;
                    }

                }
            }
        }

        private bool CompareArrays(byte[] firstArray, byte[] secondArray)
        {
            int arrLen = firstArray.Length;
            if (firstArray.Length != secondArray.Length)
                return false;
            for (int i = 0; i < arrLen; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }
    }
}
