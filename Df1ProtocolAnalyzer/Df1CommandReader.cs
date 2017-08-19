using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Df1ProtocolAnalyzer
{
    class Df1CommandReader
    {
        Df1FrameReader _dfr;
        public Df1CommandReader(Df1FrameReader dfr)
        {
            _dfr = dfr;
        }

        public IEnumerable<Df1BaseCmd> ReadCommand()
        {
            Frame commandFrame = null;
            Frame replyFrame = null;
            Df1BaseCmd df1Command = null;
            var dfr = _dfr;
            int prevTransaction = 0;

            foreach (var frame in dfr.ReadFrame())
            {
                if (frame.Type == FrameType.Reply && null == commandFrame)
                {
                    Console.WriteLine("Warning: partial command was ignored (reply transaction #{0})", frame.TransactionNumber);
                    replyFrame = commandFrame = null;
                    continue;
                }

                if (frame.Type == FrameType.Command && null != commandFrame)
                {
                    Console.WriteLine("Warning: partial command was ignored (command transaction #{0})", commandFrame.TransactionNumber);
                }

                if (frame.Type == FrameType.Command)
                {
                    commandFrame = frame;
                    continue;
                }

                if (frame.Type == FrameType.Reply)
                {
                    replyFrame = frame;
                }

                System.Diagnostics.Debug.Assert(null != commandFrame && null != replyFrame);

                if (commandFrame.TransactionNumber != replyFrame.TransactionNumber)
                {
                    Console.WriteLine("Warning: command and reply frame transaction mismatch (#{0},#{1})", 
                        commandFrame.TransactionNumber, replyFrame.TransactionNumber);
                    replyFrame = commandFrame = null;
                    continue;
                }

                if (prevTransaction > 0 && prevTransaction + 1 != commandFrame.TransactionNumber)
                {
                    Console.WriteLine("Warning: missing transactions between (#{0},#{1})",
                        prevTransaction, commandFrame.TransactionNumber);
                }

                prevTransaction = commandFrame.TransactionNumber;

                Type commandType = typeof(Df1BaseCmd);

                switch ((CommandCodes)commandFrame.CommandCode)
                {
                    case CommandCodes.Code_0F:
                        switch (commandFrame.FunctionCode)
                        {
                            case 0xA2:
                                commandType = typeof(ProtectedReadCmd);
                                break;

                            case 0xAA:
                                commandType = typeof(ProtectedWriteCmd);
                                break;
                        }
                        break;
                }

                yield return (Df1BaseCmd)Activator.CreateInstance(commandType, new object[] { commandFrame, replyFrame });

                commandFrame = replyFrame = null;
            }
        }

        
    }
}
