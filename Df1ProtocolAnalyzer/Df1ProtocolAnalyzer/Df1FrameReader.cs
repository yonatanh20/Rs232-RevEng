using System;
using System.Collections.Generic;
using System.Text;

namespace Df1ProtocolAnalyzer
{
    public enum FrameStates
    {
        Unsynced,
        InFrame,
        WaitingForAck,
        OutOfFrame,
        Error
    };

    public class Df1FrameReader
    {
        const int MAX_FRAME_SIZE = 1000;
        const int MAX_QUEUE_SIZE = 10000;


        public struct ByteRef
        {
            public byte Data;
            public DateTime Timestamp;

            public ByteRef(byte data, DateTime timestamp)
            {
                Data = data;
                Timestamp = timestamp;
            }
        }


        public class Frame
        {
            public Frame(DateTime time, Originators originator)
            {
                TimeStamp = time;
                Originator = originator;
            }

            public DateTime TimeStamp { get; set; }
            public Originators Originator { get; set; }

            int FrameIndex = 0;
            int FramedataIndex = 0;

            public bool FrameAcknowledged { get; set; } = false;
            public int FrameSource { get; set; } = 0;

            private int FrameDestination = 0;

            public int GetFrameDestination()
            {
                return FrameDestination;
            }

            public void SetFrameDestination(int value)
            {
                FrameDestination = value;
            }

            public int FrameCommand { get; set; } = 0;
            public int FrameStatusCode { get; set; } = 0;
            public int FrameTransactionNumber { get; set; } = 0;
            public int FrameFunction { get; set; } = 0;
            public int FrameAddress { get; set; } = 0;
            public int DataSize { get; set; } = 0;
            public int[] FrameData = new int[256];
            public int FrameCrcChecksum { get; set; } = 0;

            public bool AddToFrame(int _byte)
            {
                switch (FrameIndex++)
                {
                    case 0:
                        FrameSource = _byte;
                        return true;

                    case 1:
                        FrameDestination = _byte;
                        return true;

                    case 2:
                        FrameCommand = _byte;
                        return true;

                    case 3:
                        FrameStatusCode = _byte;
                        return true;

                    case 4:
                        FrameTransactionNumber = _byte;
                        return true;

                    case 5:
                        FrameTransactionNumber = FrameTransactionNumber << 8 | _byte;
                        return true;

                    case 6:
                        FrameFunction = _byte;
                        return true;

                    case 7:
                        FrameAddress = _byte;
                        return true;

                    case 8:
                        FrameAddress = FrameAddress << 8 | _byte;
                        return true;

                    case 9:
                        DataSize = _byte;
                        return true;

                    default:
                        FrameData[FramedataIndex++] = _byte;
                        return true;
                }
            }

            public bool AddCrcToFrame(int _byte)
            {
                FrameCrcChecksum = FrameAddress << 8 | _byte;
                return true;
            }

            public override string ToString()
            {
                return String.Format("{1:s} {0}({6}) Command({4})  Status {5}  " +
                    "\nAddres - {7}" +
                    "\nData Size - {8}  Data - {9}" +
                    "\n----ACK - {10} \n\n"
                    , Originator, TimeStamp,
                    FrameSource, FrameDestination, FrameStatusCode, FrameCommand.ToString("X2")
                    , FrameTransactionNumber, FrameAddress, DataSize, FramedataIndex, FrameAcknowledged);
            }
        }


        public Df1FrameReader(EZViewFileReader evfr)
        {

        }

        Queue<ByteRef> _dceQueue = new Queue<ByteRef>();
        Queue<ByteRef> _dteQueue = new Queue<ByteRef>();

        FrameStates _DCE_State = FrameStates.Unsynced;
        FrameStates _DTE_State = FrameStates.Unsynced;

        ByteRef _dceByte;
        ByteRef _dteByte;

        Frame _cur_DCE_Frame = new Frame(DateTime.MinValue, Originators.DCE);
        Frame _cur_DTE_Frame = new Frame(DateTime.MinValue, Originators.DTE);

        Queue<Frame> _DCE_Frames = new Queue<Frame>();
        Queue<Frame> _DTE_Frames = new Queue<Frame>();

        int _bytesElapsed = 0;

        public ByteRef ReadByte(EZViewFileReader evfr, Originators originator)
        {
            do
            {
                if (originator == Originators.DCE && _dceQueue.Count > 0)
                {
                    //Console.WriteLine(String.Format("Requested DCE byte = {0} \n", _dceQueue.Peek().Data));
                    return _dceQueue.Dequeue();
                }
                if (originator == Originators.DTE && _dteQueue.Count > 0)
                {
                    //Console.WriteLine(String.Format("Requested DTE byte = {0} \n", _dteQueue.Peek().Data));
                    return _dteQueue.Dequeue();
                }
                if (evfr.Read() < 0)
                    return default(ByteRef);

                switch (evfr.Originator)
                {
                    case Originators.DCE:
                        _dceQueue.Enqueue(new ByteRef(evfr.DataByte, evfr.Timestamp));
                        break;

                    case Originators.DTE:
                        _dteQueue.Enqueue(new ByteRef(evfr.DataByte, evfr.Timestamp));
                        break;
                }
            }
            while (true);
        }

        public IEnumerable<Frame>ReadFrame(EZViewFileReader evfr)
        {

            
            do
            {
                // Pop frames from given byte "streams"


                if (_DCE_State == FrameStates.Unsynced && _DTE_State == FrameStates.Unsynced)
                {
                    _dceByte = ReadByte(evfr, Originators.DCE);

                    if (_dceByte.Data == 0x10)
                    {
                        _dceByte = ReadByte(evfr, Originators.DCE);
                        if (_dceByte.Data == 0x02)
                        {
                            _cur_DCE_Frame = new Frame(_dceByte.Timestamp, Originators.DCE);
                            _DCE_State = FrameStates.InFrame;
                        }
                        else
                        {
                            // Ignore
                        }
                    }
                }

                if (_DTE_State == FrameStates.Unsynced && _DCE_State == FrameStates.WaitingForAck)
                {
                    //Catch Up
                    do
                    {
                        _dteByte = ReadByte(evfr, Originators.DTE);
                    }
                    while (_dteByte.Timestamp < _dceByte.Timestamp);

                    _DTE_State = FrameStates.OutOfFrame;
                }

                if (_DCE_State == FrameStates.InFrame)
                {
                    //Read Byte
                    _dceByte = ReadByte(evfr, Originators.DCE);
                    
                    if (_dceByte.Data == 0x10)
                    {
                        _dceByte = ReadByte(evfr, Originators.DCE);
                        
                        if (_dceByte.Data == 0x10)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _cur_DCE_Frame.AddToFrame(_dceByte.Data);
                        }
                        else if(_dceByte.Data == 0x03)
                        {
                            //This is DLE ETX meanning the first frame is finnished

                            // Adding CRC bytes
                            
                            _cur_DCE_Frame.AddCrcToFrame(ReadByte(evfr, Originators.DCE).Data);
                            _cur_DCE_Frame.AddCrcToFrame(ReadByte(evfr, Originators.DCE).Data);

                            if (CheckCrc(_cur_DCE_Frame))
                            {
                                _DCE_Frames.Enqueue(_cur_DCE_Frame);
                                _DCE_State = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                //Later and Nak cases
                                _DCE_State = FrameStates.Unsynced;
                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame. 
                            _DCE_State = FrameStates.Unsynced;
                            throw new DataMisalignedException();
                        }

                    }
                    else
                    {
                        _cur_DCE_Frame.AddToFrame(_dceByte.Data);
                    }
                }
                
                if (_DTE_State == FrameStates.InFrame)
                {
                    _dteByte = ReadByte(evfr, Originators.DTE);

                    if (_DTE_State == FrameStates.WaitingForAck)
                    {
                        _DTE_State = FrameStates.OutOfFrame;
                        _cur_DTE_Frame.FrameAcknowledged = false;
                    }
                    if (_dteByte.Data == 0x10)
                    {
                        _dteByte = ReadByte(evfr, Originators.DTE);
                        if (_dteByte.Data == 0x10)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _cur_DTE_Frame.AddToFrame(_dteByte.Data);
                        }
                        else if (_dteByte.Data == 0x03)
                        {
                            //This is DLE ETX meanning the first frame is finnished

                            // Adding CRC bytes
                            
                            _cur_DTE_Frame.AddCrcToFrame(ReadByte(evfr, Originators.DTE).Data);
                            _cur_DTE_Frame.AddCrcToFrame(ReadByte(evfr, Originators.DTE).Data);

                            if (CheckCrc(_cur_DTE_Frame))
                            {
                                _DTE_Frames.Enqueue(_cur_DTE_Frame);
                                _DTE_State = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                _DTE_State = FrameStates.Unsynced;
                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame.
                            //Later and Nak cases
                            _DTE_State = FrameStates.Unsynced;
                        }

                    }
                    else
                    {
                        if (!_cur_DTE_Frame.AddToFrame(_dteByte.Data))
                        {
                            //The Frame is too long so we will reset it
                            _DCE_State = FrameStates.Unsynced;

                        }
                    }
                }
                
                if (_DCE_State == FrameStates.WaitingForAck && _DTE_State == FrameStates.OutOfFrame)
                {

                    if (_dteByte.Data == 0x10)
                    {
                        _dteByte = ReadByte(evfr, Originators.DTE);
                        if (_dteByte.Data == 0x06)
                        {
                            _DCE_State = FrameStates.OutOfFrame;
                            _cur_DCE_Frame.FrameAcknowledged = true;
                        }
                        else if (_dteByte.Data == 0x0F)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                            throw new DataMisalignedException();
                    }
                    else
                    {
                        //One frame is acknowledged and the other is not 

                        _DTE_State = FrameStates.Unsynced;
                        _DCE_State = FrameStates.Unsynced;
                        _cur_DTE_Frame.FrameAcknowledged = false;

                    }

                }

                if (_DCE_State == FrameStates.OutOfFrame && _DTE_State == FrameStates.WaitingForAck)
                {

                    if (_dceByte.Data == 0x10)
                    {
                        _dceByte = ReadByte(evfr, Originators.DCE);

                        if (_dceByte.Data == 0x06)
                        {
                            _DTE_State = FrameStates.OutOfFrame;
                            _cur_DTE_Frame.FrameAcknowledged = true;
                        }
                        else if (_dceByte.Data == 0x0F)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                        {
                            //One frame is acknowledged and the other is not 

                            _DTE_State = FrameStates.Unsynced;
                            _DCE_State = FrameStates.Unsynced;
                            _cur_DTE_Frame.FrameAcknowledged = false;

                        }
                    }
                    else
                    {
                        //One frame is acknowledged and the other is not 

                        _DTE_State = FrameStates.Unsynced;
                        _DCE_State = FrameStates.Unsynced;
                        _cur_DTE_Frame.FrameAcknowledged = false;

                    }
                }

                if (_DTE_State == FrameStates.OutOfFrame && _DCE_State == FrameStates.OutOfFrame)
                {

                    if (_dteByte.Timestamp < _dceByte.Timestamp)
                    {
                        _dteByte = ReadByte(evfr, Originators.DTE);

                        if (_dteByte.Data == 0x10)
                        {
                            _dteByte = ReadByte(evfr, Originators.DTE);
                            if (_dteByte.Data == 0x02)
                            {
                                _DTE_State = FrameStates.OutOfFrame;
                                _cur_DTE_Frame.FrameAcknowledged = false;

                                _cur_DTE_Frame = new Frame(_dteByte.Timestamp, Originators.DTE);
                                _DTE_State = FrameStates.InFrame;
                            }
                        }
                    }
                    else
                    {
                        _dceByte = ReadByte(evfr, Originators.DCE);

                        if (_dceByte.Data == 0x10)
                        {
                            _dceByte = ReadByte(evfr, Originators.DCE);
                            if (_dceByte.Data == 0x02)
                            {
                                _DTE_State = FrameStates.OutOfFrame;
                                _cur_DTE_Frame.FrameAcknowledged = false;

                                _cur_DCE_Frame = new Frame(_dceByte.Timestamp, Originators.DCE);
                                _DCE_State = FrameStates.InFrame;
                            }
                        }
                    }
                    

                }

                if (_DCE_Frames.Count != 0 && _DTE_Frames.Count != 0)
                {
                    Console.WriteLine("FRAME");
                    if (_DCE_Frames.Peek().TimeStamp < _DTE_Frames.Peek().TimeStamp)
                    {
                        //Pop frame and return
                        yield return _DCE_Frames.Dequeue();

                    }
                    else
                    {
                        //Pop frame and return
                        yield return _DTE_Frames.Dequeue();
                    }
                }

            }
            while (true);
        }


        int FrameErrors { get; set; }

        bool CheckCrc(Frame frame)
        {

            return true;// iff CRC is correct etc.

        }
    }

    }

