using System;

namespace Df1ProtocolAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dfpa <ezview-file.dat> <raw|frame|command");
                return;
            }

            if (args.Length > 1 && args[1].Equals("raw", StringComparison.OrdinalIgnoreCase))
            {
                var evfr = new EZViewFileReader(args[0]);
                var curOrg = Originators.Error;

                while (evfr.Read() >= 0)
                {
                    if (curOrg != evfr.Originator)
                    {
                        Console.WriteLine(evfr.Timestamp);
                        curOrg = evfr.Originator;
                    }   

                    var valName = Enum.IsDefined(typeof(Df1TransmissionSymbols), (int)evfr.DataByte) ? 
                        Enum.GetName(typeof(Df1TransmissionSymbols), evfr.DataByte) : evfr.DataByte.ToString("X2");

                    Console.WriteLine($"{evfr.Originator.ToString()}\t{valName}");
                }

                Console.WriteLine("");
            }
            else
            {
                var evfr = new EZViewFileReader(args[0]);

                var dfr = new Df1FrameReader(evfr);
                    
                foreach (var frame in dfr.ReadFrame(evfr))
                {
                    Console.WriteLine(frame.ToString());
                }

                Console.WriteLine("");
            }
        }
    }
}