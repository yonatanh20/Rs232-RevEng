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

                    var valName = Enum.IsDefined(typeof(TxSymbols), (int)evfr.DataByte) ?
                        Enum.GetName(typeof(TxSymbols), evfr.DataByte) : evfr.DataByte.ToString("X2");

                    Console.WriteLine($"{evfr.Originator.ToString()}\t{valName}");
                }

                Console.WriteLine("");
            }
            else if (args.Length > 1 && args[1].Equals("frame", StringComparison.OrdinalIgnoreCase))
            {
                var evfr = new EZViewFileReader(args[0]);

                var dfr = new Df1FrameReader(evfr);

                foreach (var frame in dfr.ReadFrame())
                {
                    Console.WriteLine(frame.ToString());
                }

                Console.WriteLine("");
            }
            else if (args.Length > 1 && args[1].Equals("command", StringComparison.OrdinalIgnoreCase))
            {
                var evfr = new EZViewFileReader(args[0]);

                var dfr = new Df1FrameReader(evfr);

                var dcr = new Df1CommandReader(dfr);

                foreach (var command in dcr.ReadCommand())
                {
                    Console.WriteLine(command.ToString());
                    //if (analyzedFrame.FunctionType == FunctionTypes.ProtectedWrite)
                    //    Console.WriteLine(analyzedFrame.ToString());
                    //else if (analyzedFrame.FunctionType == FunctionTypes.ProtectedRead)
                    //    //if (analyzedFrame.CommandFrame.FrameData[0] >= 16) ;
                    //    Console.WriteLine(analyzedFrame.ToString());
                }
            }

        }
    }
}