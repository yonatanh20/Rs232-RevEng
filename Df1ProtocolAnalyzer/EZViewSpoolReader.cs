using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Threading;

namespace Df1ProtocolAnalyzer
{

    public class EZViewSpoolReader : IRS232Reader
    {
        const string _pattern = @"^([0-9]+)\t([^ ]+ [^\t]+)\t([^\t]*)?\t([^\t]*)?\t([^\t]*)?\t([^\t]*)?\t$";
        const string _dateFormat = @"MM/dd/yy HH:mm:ssffffff";
        string _filepath = "";

        Regex _re = new Regex(_pattern);
        Match _reMatch;
        GroupCollection _data;
        
        FileStream _file;

        public byte DataByte { get; private set; } = 0;
        public Originators Originator { get; private set; }
        public DateTime Timestamp { get; private set; }

        public EZViewSpoolReader(string filepath)
        {
            _filepath = filepath;
            _file = File.Open(_filepath, FileMode.Open,FileAccess.Read, FileShare.ReadWrite);
        }

        public IEnumerable<ByteDef> Read()
        {
            string line;

            var sr = new StreamReader(_file);
            while (true)
            {
                if (sr.EndOfStream)
                {
                    
                    var pos = _file.Position;
                    Thread.Sleep(2000);

                    _file.Flush();
                    _file.Seek(0, SeekOrigin.End);
                    var endPos = _file.Position;
                    if (endPos <= pos)
                        continue;
                    _file.Seek(pos, SeekOrigin.Begin);
                    sr.DiscardBufferedData();
                    
                }

                line = sr.ReadLine();
                _reMatch = _re.Match(line);

                if (_reMatch.Success)
                {
                    _data = _reMatch.Groups;
                    string date = _data[2].ToString().Replace(".", "");
                    
                    try
                    {
                        Timestamp = DateTime.ParseExact(date, _dateFormat, Thread.CurrentThread.CurrentCulture);
                    }
                    catch
                    {
                    }
                    if (_data[3].ToString() != "")
                    {
                        DataByte = byte.Parse(_data[3].ToString(), NumberStyles.HexNumber);
                        Originator = Originators.DCE;
                        yield return new ByteDef(DataByte, Timestamp, Originator);
                    }
                    else if (_data[5].ToString() != "")
                    {
                        DataByte = byte.Parse(_data[5].ToString(), NumberStyles.HexNumber);
                        Originator = Originators.DTE;
                        yield return new ByteDef(DataByte, Timestamp, Originator);
                    }
                    else
                        Console.WriteLine("No data");
                }
                else
                {
                    Console.WriteLine("Bad input line.");

                }


            }

        }

       
        
    }
}
