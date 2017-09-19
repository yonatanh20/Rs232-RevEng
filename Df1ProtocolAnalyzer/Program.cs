using System;

namespace Df1ProtocolAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dfpa <ezview-file.dat> <raw|frame|command|changes|playback> <reg|spool>");
                return;
            }

            if (args.Length > 2 && args[1].Equals("raw", StringComparison.OrdinalIgnoreCase))
            {
                IRS232Reader evr;
                if (args[2].Equals("reg", StringComparison.OrdinalIgnoreCase))
                    evr = new EZViewFileReader(args[0]);
                else 
                    evr = new EZViewSpoolReader(args[0]);
                var curOrg = Originators.Error;

                foreach (var byteDeff in evr.Read())
                {
                    if (curOrg != byteDeff.Originator)
                    {
                        Console.WriteLine(byteDeff.Timestamp);
                        curOrg = byteDeff.Originator;
                    }
                    Console.WriteLine($"{byteDeff.Originator} - {byteDeff.DataByte}");
                }
                
            }
            else if (args.Length > 1 && args[1].Equals("frame", StringComparison.OrdinalIgnoreCase))
            {
                IRS232Reader evr;

                if (args[2].Equals("reg", StringComparison.OrdinalIgnoreCase))
                    evr = new EZViewFileReader(args[0]);
                else
                    evr = new EZViewSpoolReader(args[0]);

                var dfr = new Df1FrameReader(evr);

                foreach (var frame in dfr.ReadFrame())
                {
                    Console.WriteLine(frame.ToString());
                }
            }
            else if (args.Length > 1 && args[1].Equals("command", StringComparison.OrdinalIgnoreCase))
            {
                IRS232Reader evr;

                if (args[2].Equals("reg", StringComparison.OrdinalIgnoreCase))
                    evr = new EZViewFileReader(args[0]);
                else
                    evr = new EZViewSpoolReader(args[0]);

                var dfr = new Df1FrameReader(evr);

                var dcr = new Df1CommandReader(dfr);

                foreach (var command in dcr.ReadCommand())
                {
                    Console.WriteLine(command.ToString());
                }
            }
            else if (args.Length > 1 && args[1].Equals("changes", StringComparison.OrdinalIgnoreCase))
            {
                IRS232Reader evr;

                if (args[2].Equals("reg", StringComparison.OrdinalIgnoreCase))
                    evr = new EZViewFileReader(args[0]);
                else
                    evr = new EZViewSpoolReader(args[0]);

                var dfr = new Df1FrameReader(evr);

                var dcr = new Df1CommandReader(dfr);

                var ppl = new PlcPlayback(dcr);

                foreach (var prc in ppl.PlcChanges())
                {
                    Console.WriteLine(prc.ToString());
                }
            }
            else if (args.Length > 1 && args[1].Equals("playback", StringComparison.OrdinalIgnoreCase))
            {
                var dbu = new DBUtil();

                PlcControl.DbUtil = dbu;

                IRS232Reader evr;

                if (args[2].Equals("reg", StringComparison.OrdinalIgnoreCase))
                    evr = new EZViewFileReader(args[0]);
                else
                    evr = new EZViewSpoolReader(args[0]);

                var dfr = new Df1FrameReader(evr);

                var dcr = new Df1CommandReader(dfr);

                var ppl = new PlcPlayback(dcr);     
                
                foreach (var prc in ppl.PlcChanges())
                {
                    foreach (var changedbit in prc.ChangedBits)
                    {
                        var plc = new PlcControl(prc, changedbit);
                        Console.WriteLine(plc.ToString());
                    }
                }
            }

        }
    }
}