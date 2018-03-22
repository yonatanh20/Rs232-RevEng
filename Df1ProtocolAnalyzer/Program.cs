using System;

namespace Df1ProtocolAnalyzer
{
    class Program
    {
        public enum FileType
        {
            SpoolFile,
            FinishedFile
        }
        static void Main(string[] args)
        {
            
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dfpa <ezview-file.dat|ezview-spool-file.txt> <raw|frame|command|changes|playback>");
                return;
            }

            //Figure out File Type to start parsing

            FileType file;
            if (args[0].EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                file = FileType.SpoolFile;
            }
            else if (args[0].EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
            {
                file = FileType.FinishedFile;
            }
            else
            {
                Console.WriteLine("Usage: dfpa <ezview-file.dat|ezview-spool-file.txt> <raw|frame|command|changes|playback>");
                return;
            }

            //In all caes there is a need to start parsing
            //The update removes code duplication

            IRS232Reader evr;
            if (file.Equals(FileType.FinishedFile))
                evr = new EZViewFileReader(args[0]);
            else
                evr = new EZViewSpoolReader(args[0]);

            
            if (args.Length == 2 && args[1].Equals("raw", StringComparison.OrdinalIgnoreCase))

            {

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


            var dfr = new Df1FrameReader(evr);

            if (args.Length > 1 && args[1].Equals("frame", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var frame in dfr.ReadFrame())
                {
                    Console.WriteLine(frame.ToString());
                }
            }

            var dcr = new Df1CommandReader(dfr);

            if (args.Length > 1 && args[1].Equals("command", StringComparison.OrdinalIgnoreCase))
            {

                foreach (var command in dcr.ReadCommand())
                {
                    Console.WriteLine(command.ToString());
                }
            }
        
            var ppl = new PlcPlayback(dcr);

            if (args.Length > 1 && args[1].Equals("changes", StringComparison.OrdinalIgnoreCase))
            {

                foreach (var prc in ppl.PlcChanges())
                {
                    Console.WriteLine(prc.ToString());
                }
            }

            if (args.Length > 1 && args[1].Equals("playback", StringComparison.OrdinalIgnoreCase))
            {
                var dbu = new DBUtil();

                PlcControl.DbUtil = dbu;
                
                
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