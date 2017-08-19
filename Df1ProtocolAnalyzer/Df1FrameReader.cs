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

        bool _outOfData = false;

        FrameStates _dceState = FrameStates.Unsynced;
        FrameStates _dteState = FrameStates.Unsynced;

        ByteRef _dceByte;
        ByteRef _dteByte;

		Frame _dceFrame;
		Frame _dteFrame;

		EZViewFileReader _evfr;

		public Df1FrameReader(EZViewFileReader evfr)
		{
			_evfr = evfr;
		}

		#region Dual channel ByteRef queue reader

		Queue<ByteRef> _dceQueue = new Queue<ByteRef>();
		Queue<ByteRef> _dteQueue = new Queue<ByteRef>();

		public delegate ByteRef QueueOperDelegate();
		public ByteRef PeekByte(EZViewFileReader evfr, Originators originator)
		{
			QueueOperDelegate oper;
			if (originator == Originators.DCE)
				oper = _dceQueue.Peek;
			else
				oper = _dteQueue.Peek;

			return QueueOper(evfr, originator, oper);
		}
		public ByteRef PopByte(EZViewFileReader evfr, Originators originator)
		{
			QueueOperDelegate oper;
			if (originator == Originators.DCE)
				oper = _dceQueue.Dequeue;
			else
				oper = _dteQueue.Dequeue;

			return QueueOper(evfr, originator, oper);
		}

		public ByteRef QueueOper(EZViewFileReader evfr, Originators originator, QueueOperDelegate oper)
		{
			do
			{
				if (originator == Originators.DCE && _dceQueue.Count > 0)
				{
					//Console.WriteLine(String.Format("Requested DCE byte = {0} \n", _dceQueue.Peek().Data));
					return oper();
				}
				if (originator == Originators.DTE && _dteQueue.Count > 0)
				{
					//Console.WriteLine(String.Format("Requested DTE byte = {0} \n", _dteQueue.Peek().Data));
					return oper();
				}
                if (evfr.Read() < 0)
                {
                    _outOfData = true;
                    return default(ByteRef);
                }
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
        #endregion



        #region Dual channel Frame extractor
        public delegate void FrameOperDelegate();

        public IEnumerable<Frame>ReadFrame()
        {
			EZViewFileReader evfr = _evfr;

			do
            {
                if (_dceState == FrameStates.Unsynced && _dteState == FrameStates.Unsynced)
                {
                    _dceByte = PopByte(evfr, Originators.DCE);

                    if ((TxSymbols)_dceByte.Data == TxSymbols.DLE)
                    {
                        _dceByte = PeekByte(evfr, Originators.DCE);
                        if ((TxSymbols)_dceByte.Data == TxSymbols.STX)
                        {
                            _dceByte = PopByte(evfr, Originators.DCE);
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
                        _dteByte = PopByte(evfr, Originators.DTE);
                    }
                    while (_dteByte.Timestamp < _dceByte.Timestamp);

                    _dteState = FrameStates.GivingAck;
                }

                else if (_dceState == FrameStates.InFrame)
                {
                    _dceByte = PopByte(evfr, Originators.DCE);
                    
                    if ((TxSymbols)_dceByte.Data == TxSymbols.DLE)
                    {
                        _dceByte = PopByte(evfr, Originators.DCE);
                        
                        if ((TxSymbols)_dceByte.Data == TxSymbols.DLE)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _dceFrame.AddToFrame(_dceByte.Data);
                        }
                        else if((TxSymbols)_dceByte.Data == TxSymbols.ETX)
                        {
                            //This is DLE ETX meaning the first frame is finished

                            // Adding CRC bytes
                            
                            _dceFrame.AddCrcToFrame(PopByte(evfr, Originators.DCE).Data);
                            _dceFrame.AddCrcToFrame(PopByte(evfr, Originators.DCE).Data);

                            if (CheckCrc(_dceFrame))
                            {
                                _dceState = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                //Later and Nak cases
                                _dceState = FrameStates.Unsynced;
                                
                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame. 
                            _dceState = FrameStates.Unsynced;
                            
                        }

                    }
                    else
                    {
                        _dceFrame.AddToFrame(_dceByte.Data);
                    }
                }

                else if (_dteState == FrameStates.InFrame)
                {
                    _dteByte = PopByte(evfr, Originators.DTE);

                    if (_dteState == FrameStates.WaitingForAck)
                    {
                        _dteState = FrameStates.OutOfFrame;
                        _dteFrame.FrameAcknowledged = false;
                    }
                    if ((TxSymbols)_dteByte.Data == TxSymbols.DLE)
                    {
                        _dteByte = PopByte(evfr, Originators.DTE);

                        if ((TxSymbols)_dteByte.Data == TxSymbols.DLE)
                        {
                            //This is DLE DLE and it's only considered a regular 10
                            _dteFrame.AddToFrame(_dteByte.Data);
                        }
                        else if ((TxSymbols)_dteByte.Data == TxSymbols.ETX)
                        {
                            //This is DLE ETX meanning the first frame is finnished

                            // Adding CRC bytes
                            
                            _dteFrame.AddCrcToFrame(PopByte(evfr, Originators.DTE).Data);
                            _dteFrame.AddCrcToFrame(PopByte(evfr, Originators.DTE).Data);

                            if (CheckCrc(_dteFrame))
                            {
                                _dteState = FrameStates.WaitingForAck;
                            }
                            else
                            {
                                _dteState = FrameStates.Unsynced;
                                
                            }
                        }
                        else
                        {
                            //Unexpected byte after DLE meanning we need to reset the frame.
                            //Later and Nak cases
                            _dteState = FrameStates.Unsynced;
                            
                        }

                    }
                    else
                    {
                        if (!_dteFrame.AddToFrame(_dteByte.Data))
                        {
                            //The Frame is too long so we will reset it
                            
                            _dceState = FrameStates.Unsynced;

                        }
                    }
                }

                else if (_dceState == FrameStates.WaitingForAck && _dteState == FrameStates.GivingAck)
                {


                    if ((TxSymbols)_dteByte.Data == TxSymbols.DLE)
                    {
                        _dteByte = PopByte(evfr, Originators.DTE);
                        if ((TxSymbols)_dteByte.Data == TxSymbols.ACK)
                        {
                            _dceByte = PopByte(evfr, Originators.DCE);
                            _dceState = FrameStates.GivingAck;
                            _dteState = FrameStates.OutOfFrame;
                            _dceFrame.FrameAcknowledged = true;
                            
                            yield return _dceFrame;
							_dceFrame = null;
						}
                        else if ((TxSymbols)_dteByte.Data == TxSymbols.NAK)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                        {
                            
                            _dteState = FrameStates.Unsynced;
                            _dceState = FrameStates.Unsynced;
                            _dteFrame.FrameAcknowledged = false;

                        }
                    }
                    else
                    {
                        
                        _dteState = FrameStates.Unsynced;
                        _dceState = FrameStates.Unsynced;
                        _dteFrame.FrameAcknowledged = false;

                    }

                }

                else if (_dceState == FrameStates.GivingAck && _dteState == FrameStates.WaitingForAck)
                {

                     if ((TxSymbols)_dceByte.Data == TxSymbols.DLE)
                    {
                        _dceByte = PopByte(evfr, Originators.DCE);

                        if ((TxSymbols)_dceByte.Data == TxSymbols.ACK)
                        {
                            _dteByte = PopByte(evfr, Originators.DTE);
                            _dteState = FrameStates.GivingAck;
                            _dceState = FrameStates.OutOfFrame;
                            _dteFrame.FrameAcknowledged = true;
                            yield return _dteFrame;
							_dteFrame = null;
						}
                        else if ((TxSymbols)_dceByte.Data == TxSymbols.NAK)
                        {
                            //NAK
                            //Add code
                            //
                        }
                        else
                        {
                            
                            _dteState = FrameStates.Unsynced;
                            _dceState = FrameStates.Unsynced;
                            _dteFrame.FrameAcknowledged = false;

                        }
                    }
                    else
                    {
                        
                        _dteState = FrameStates.Unsynced;
                        _dceState = FrameStates.Unsynced;
                        _dteFrame.FrameAcknowledged = false;

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
            EZViewFileReader evfr = _evfr;

             ByteRef byteref = PopByte(evfr, originator);

            if ((TxSymbols)byteref.Data == TxSymbols.DLE)
            {
                byteref = PopByte(evfr, originator);
                if ((TxSymbols)byteref.Data == TxSymbols.STX)
                {
                    return true;
                }
            }
            
            return false;
        }

        bool GivingAcknowledge(Originators originator, ByteRef byteRef, Frame frame)
        {

            EZViewFileReader evfr = _evfr;
            
            if ((TxSymbols)byteRef.Data == TxSymbols.DLE)
            {
                byteRef = PopByte(evfr, originator);
                if ((TxSymbols)byteRef.Data == TxSymbols.ACK)
                {
                    originator = originator == Originators.DCE ? Originators.DTE : Originators.DCE;
                    byteRef = PopByte(evfr, originator);
                    frame.FrameAcknowledged = true;
                    return true;
                }
                else if ((TxSymbols)byteRef.Data == TxSymbols.NAK)
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

