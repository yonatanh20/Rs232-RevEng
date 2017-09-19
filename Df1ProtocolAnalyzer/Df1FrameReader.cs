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
        GivingAck,
        OutOfFrame,
        Error
    };

    public class Df1FrameReader
    {

        bool _outOfData = false;

        FrameStates _dceState = FrameStates.Unsynced;
        FrameStates _dteState = FrameStates.Unsynced;

        ByteDef _dceByte;
        ByteDef _dteByte;

		Frame _dceFrame;
		Frame _dteFrame;

        IRS232Reader _evr;
        IEnumerator<ByteDef> _byteDefReader;

        public Df1FrameReader(IRS232Reader evr)
		{
			_evr = evr;
            _byteDefReader = evr.Read().GetEnumerator();

        }

		#region Dual channel ByteRef queue reader

		Queue<ByteDef> _dceQueue = new Queue<ByteDef>();
		Queue<ByteDef> _dteQueue = new Queue<ByteDef>();

		public delegate ByteDef QueueOperDelegate();
		public ByteDef PeekByte(Originators originator)
		{
			QueueOperDelegate oper;
			if (originator == Originators.DCE)
				oper = _dceQueue.Peek;
			else
				oper = _dteQueue.Peek;

			return QueueOper(originator, oper);
		}
		public ByteDef PopByte( Originators originator)
		{
			QueueOperDelegate oper;
			if (originator == Originators.DCE)
				oper = _dceQueue.Dequeue;
			else
				oper = _dteQueue.Dequeue;

			return QueueOper(originator, oper);
		}

        public ByteDef QueueOper(Originators originator, QueueOperDelegate oper)
        {
            while (!_outOfData)
            {
                if (originator == Originators.DCE && _dceQueue.Count > 0)
                {
                    return oper();
                }
                if (originator == Originators.DTE && _dteQueue.Count > 0)
                {
                    return oper();
                }
                
                if (_byteDefReader.MoveNext())
                {
                    var byteDef = _byteDefReader.Current;
                    switch (byteDef.Originator)
                    {
                        case Originators.DCE:
                            _dceQueue.Enqueue(byteDef);
                            break;

                        case Originators.DTE:
                            _dteQueue.Enqueue(byteDef);
                            break;
                    }
                }
                else
                    _outOfData = true;
            }
            return default(ByteDef);
        } 

        
        #endregion



        #region Dual channel Frame extractor
        public delegate void FrameOperDelegate();

        public IEnumerable<Frame>ReadFrame()
        {
            IRS232Reader evr = _evr;
			do
            {
                
                if (_dceState == FrameStates.Unsynced && _dteState == FrameStates.Unsynced)
                {
                    _dceByte = PopByte(Originators.DCE);

                    if ((TxSymbols)_dceByte.DataByte == TxSymbols.DLE)
                    {
                        _dceByte = PeekByte(Originators.DCE);
                        if ((TxSymbols)_dceByte.DataByte == TxSymbols.STX)
                        {
                            _dceByte = PopByte(Originators.DCE);
                            _dceFrame = new Frame(_dceByte.Timestamp, Originators.DCE);
                            _dceState = FrameStates.InFrame;
                        }
                        else
                        {
                            // Ignore
                        }
                    }
                }

                else if (_dteState == FrameStates.Unsynced && _dceState == FrameStates.WaitingForAck)
                {
                    //Catch Up
                    do
                    {  
                        _dteByte = PopByte(Originators.DTE);
                    }
                    while (_dteByte.Timestamp < _dceByte.Timestamp);

                    _dteState = FrameStates.GivingAck;
                }

                else if (_dceState == FrameStates.InFrame)
                {
                    _dceByte = PopByte(Originators.DCE);
                    
                    if ((TxSymbols)_dceByte.DataByte == TxSymbols.DLE)
                    {
                        _dceByte = PopByte(Originators.DCE);
                        
                        if ((TxSymbols)_dceByte.DataByte == TxSymbols.DLE)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _dceFrame.AddToFrame(_dceByte.DataByte);
                        }
                        else if((TxSymbols)_dceByte.DataByte == TxSymbols.ETX)
                        {
                            //This is DLE ETX meaning the first frame is finished

                            // Adding CRC bytes
                            
                            _dceFrame.AddCrcToFrame(PopByte(Originators.DCE).DataByte);
                            _dceFrame.AddCrcToFrame(PopByte(Originators.DCE).DataByte);

                            if (CheckCrc(_dceFrame))
                            {
                                _dceState = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                //Later and Nak cases
                                _dceState = FrameStates.Unsynced;
                                _dteState = FrameStates.Unsynced;

                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame. 
                            _dceState = FrameStates.Unsynced;
                            _dteState = FrameStates.Unsynced;

                        }

                    }
                    else
                    {
                        _dceFrame.AddToFrame(_dceByte.DataByte);
                    }
                }

                else if (_dteState == FrameStates.InFrame)
                {
                    _dteByte = PopByte(Originators.DTE);

                    if (_dteState == FrameStates.WaitingForAck)
                    {
                        _dteState = FrameStates.OutOfFrame;
                        _dteFrame.FrameAcknowledged = false;
                    }
                    if ((TxSymbols)_dteByte.DataByte == TxSymbols.DLE)
                    {
                        _dteByte = PopByte(Originators.DTE);

                        if ((TxSymbols)_dteByte.DataByte == TxSymbols.DLE)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _dteFrame.AddToFrame(_dteByte.DataByte);
                        }
                        else if ((TxSymbols)_dteByte.DataByte == TxSymbols.ETX)
                        {
                            //This is DLE ETX meanning the first frame is finnished

                            // Adding CRC bytes
                            
                            _dteFrame.AddCrcToFrame(PopByte(Originators.DTE).DataByte);
                            _dteFrame.AddCrcToFrame(PopByte(Originators.DTE).DataByte);

                            if (CheckCrc(_dteFrame))
                            {
                                _dteState = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                _dceState = FrameStates.Unsynced;
                                _dteState = FrameStates.Unsynced;

                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame.
                            //Later and Nak cases
                            _dceState = FrameStates.Unsynced;
                            _dteState = FrameStates.Unsynced;

                        }

                    }
                    else
                    {
                        if (!_dteFrame.AddToFrame(_dteByte.DataByte))
                        {
                            //The Frame is too long so we will reset it
                            
                            _dceState = FrameStates.Unsynced;
                            _dteState = FrameStates.Unsynced;

                        }
                    }
                }

                else if (_dceState == FrameStates.WaitingForAck && _dteState == FrameStates.GivingAck)
                {


                    if ((TxSymbols)_dteByte.DataByte == TxSymbols.DLE)
                    {
                        _dteByte = PopByte(Originators.DTE);
                        if ((TxSymbols)_dteByte.DataByte == TxSymbols.ACK)
                        {
                            _dceByte = PopByte(Originators.DCE);
                            _dceState = FrameStates.GivingAck;
                            _dteState = FrameStates.OutOfFrame;
                            _dceFrame.FrameAcknowledged = true;

                            yield return _dceFrame;
                            _dceFrame = null;

                        }
                        else if ((TxSymbols)_dteByte.DataByte == TxSymbols.NAK)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                        {
                            
                            _dteState = FrameStates.Unsynced;
                            _dceState = FrameStates.Unsynced;

                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to give ack to DCE frame:");
                        Console.WriteLine($"{_dceFrame.ToString()}");

                        _dteState = FrameStates.Unsynced;
                        _dceState = FrameStates.Unsynced;

                    }

                }

                else if (_dceState == FrameStates.GivingAck && _dteState == FrameStates.WaitingForAck)
                {

                     if ((TxSymbols)_dceByte.DataByte == TxSymbols.DLE)
                    {
                        _dceByte = PopByte(Originators.DCE);

                        if ((TxSymbols)_dceByte.DataByte == TxSymbols.ACK)
                        {
                            _dteByte = PopByte(Originators.DTE);
                            _dteState = FrameStates.GivingAck;
                            _dceState = FrameStates.OutOfFrame;
                            _dteFrame.FrameAcknowledged = true;
                            yield return _dteFrame;
                            _dteFrame = null;


                        }
                        else if ((TxSymbols)_dceByte.DataByte == TxSymbols.NAK)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                        {
                            
                            _dteState = FrameStates.Unsynced;
                            _dceState = FrameStates.Unsynced;

                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to give ack to DTE frame:");
                        Console.WriteLine($"{_dteFrame.ToString()}");
                        _dteState = FrameStates.Unsynced;
                        _dceState = FrameStates.Unsynced;

                    }
                }

                else if (_dteState == FrameStates.OutOfFrame || _dceState == FrameStates.OutOfFrame)
                {
                    if (_dteState == FrameStates.OutOfFrame)
                    {
                        if (ParseNewFrame(Originators.DTE))
                        {
                            _dteFrame = new Frame(_dteByte.Timestamp, Originators.DTE);
                            _dteState = FrameStates.InFrame;
                        }
                    }
                    else
                    {
                        if (ParseNewFrame(Originators.DCE))
                        {
                            _dceFrame = new Frame(_dceByte.Timestamp, Originators.DCE);
                            _dceState = FrameStates.InFrame;
                        }
                    }
					

                }


            }
            while (!_outOfData);
        }

        bool ParseNewFrame(Originators originator)
        {
            IRS232Reader evr = _evr;

             ByteDef byteref = PopByte(originator);

            if ((TxSymbols)byteref.DataByte == TxSymbols.DLE)
            {
                byteref = PopByte(originator);
                if ((TxSymbols)byteref.DataByte == TxSymbols.STX)
                {
                    return true;
                }
            }
            
            return false;
        }

        bool GivingAcknowledge(Originators originator, ByteDef byteRef, Frame frame)
        {

            IRS232Reader evr = _evr;
            
            if ((TxSymbols)byteRef.DataByte == TxSymbols.DLE)
            {
                byteRef = PopByte(originator);
                if ((TxSymbols)byteRef.DataByte == TxSymbols.ACK)
                {
                    originator = originator == Originators.DCE ? Originators.DTE : Originators.DCE;
                    byteRef = PopByte(originator);
                    frame.FrameAcknowledged = true;
                    return true;
                }
                else if ((TxSymbols)byteRef.DataByte == TxSymbols.NAK)
                {
                    //NAK
                    //Add code
                    //
                    frame.FrameAcknowledged = false;
                    return false;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #endregion
        bool CheckCrc(Frame frame)
        {

            return true;// iff CRC is correct etc.

        }
    }

    }

