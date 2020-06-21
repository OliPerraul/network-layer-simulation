using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TP1
{
    //public enum WindowStatusType
    //{
    //    ReadyToReceive,
    //    Retransmission
    //}

    //public struct WindowStatus
    //{
    //    public bool IsRetransmission;
    //    public int FrameId;
    //}

    public class Window
    {

        //This function checks if value is within a cyclic window.
        //This means that if ub < lb, then ub has wrapped around the end of an array and continued at the beginning.
        public static bool Between(int seq, int seq_lb, int seq_ub) //ensures that value is within lb and ub
        {
            bool x = (seq_lb <= seq) && (seq < seq_ub);
            bool y = (seq_ub < seq_lb) && (seq_lb <= seq);
            bool z = (seq < seq_ub) && (seq_ub < seq_lb);

            return x || y || z;
        }


        //
        //sf : sender first outsanding frame
        //sn : sender next frame
        //rn : receiver next frame
        //sw : sender window size(atmost n/2)
        //rw : receiver window size(atmost n/2)

        public Frame[] _frames;

        private int _size;
        public int Size => _size;


        public Window(int windowSize)
        {
            _size = windowSize;
            _frames = new Frame[_size];
        }
    }

    public class A_SenderWindow : Window
    {
        public int _currentSize = 0;

        //sf : sender first outsanding frame
        public int _lowerBound = 0;

        public A_SenderWindow(int windowSize) : base(windowSize)
        {

        }

        public void Update(Frame ackFrame, A2_TimerManager timerManager)
        {
            // Purge all until ACK frame
            // Stop all timer until ACK frame
            //_window.Slide(ackFrame)                                
            for (int i = 0; i < Parameters.MaxSequenceID; i++)
            {
                int j = (_lowerBound + i) % Size;
                if (_frames[j].SequenceID == ackFrame.SequenceID)
                {
                    // Stop timer of frames we have acked
                    for (int k = 0; k < i; k++)
                    {
                        int l = (_lowerBound + k) % Size;
                        timerManager.Stop(l);
                    }

                    Slide(i);
                    break;
                }
            }           
        }

        public int Store(Frame frame)
        {
            int idx = (_lowerBound + _currentSize) % Size;

            _frames[idx] = frame;
            _currentSize++;

            return idx;
        }

        public Frame GetByIndex(int frameIndex)
        {            
            return _frames[frameIndex];
        }

        public Frame GetBySequenceId(int sequenceID)
        {
            for (int i = 0; i < _frames.Length; i++)
            {
                if (_frames[i].SequenceID == sequenceID)
                    return _frames[i];
            }

            return new Frame { Type = FrameType.None };
        }

        private void Slide(int amount)
        {            
            _lowerBound = (_lowerBound + amount) % Size;
            _currentSize -= amount;            
        }

        public void Slide(Frame ack)
        {

        }

        public bool Full
        {
            get
            {
                return _currentSize >= Size;
            }
        }
    }


    // NOTE: We use Frame.Type == None to signify if arrived or empty in the receiving window

    public class B_ReceiverWindow : Window
    {
        // keeps track of the sequence number of the earliest frame it has not received, 
        // and sends that number with every acknowledgement (ACK) it sends
        // NAK causes retransmission of oldest un-acked frame

        protected int _lowerBound = 0;
        protected int _upperBound = 0;
        protected int _nextSequencedID = 0;

        public B_ReceiverWindow(int windowSize) : base(windowSize)
        {
            _lowerBound = 0;
            _upperBound = windowSize - 1;

            // ASSUME: window size is smaller than sq_max
            // TODO: Assert
            // Initial sequence id
            _nextSequencedID = windowSize - 1;
            for (int i = 0; i < windowSize; i++)
            {
                _frames[i].SequenceID = i;
            }
        }

        public void Slide(int amount)
        {
            // Free slid by entries
            for (int i = 0; i < amount; i++)
            {
                int j = (_lowerBound + i) % Size;
                _frames[j].Type = FrameType.None;
            }
            _lowerBound = (_lowerBound + amount) % Size;

            for (int i = 1; i < amount + 1; i++)
            {
                int j = (_upperBound + i) % Size;
                _frames[j].SequenceID = (_nextSequencedID + i) % Parameters.MaxSequenceID;
            }
            _upperBound = (_upperBound + amount) % Size;
            _nextSequencedID = (_nextSequencedID + amount) % Parameters.MaxSequenceID;
        }

        // Store and obtain status
        public void Update(Frame frame, out Frame ack, out List<Frame> readyToSendUp)
        {
            readyToSendUp = new List<Frame>();
            ack = new Frame { Type = FrameType.None };
            // Case 1:
            // send ack with sequence of next expected frame in the window r_n + 1
            // If such frame will fill receiving window send the entire window to network layer
            // If such frame complete contiguous set of frames, send contiguous and slide window pointer
            if (frame.SequenceID == _frames[_lowerBound].SequenceID)
            {
                // Slide window
                // Get the next expected frame (Frame which hasnt been received)
                // add all frames which window passed by into `readytosendup`
                for (int i = 0; i < Size; i++)
                {
                    int j = (_lowerBound + i) % Size;
                    if (_frames[j].Type == FrameType.None)
                    {
                        readyToSendUp.Add(frame);
                        Slide(i + 1);
                        break;
                    }
                    else
                    {
                        readyToSendUp.Add(_frames[i]);
                    }
                }

                // Create ACK frame to send back
                ack = new Frame
                {
                    SequenceID = _frames[_lowerBound].SequenceID,
                    // If last frame is stored,
                    // send last frame aknowledgment
                    // otherwise send aknowledgment
                    Type = frame.Type == FrameType.EndOfTransmission ?
                        FrameType.ACK_EndOfTransmission :
                        FrameType.ACK
                };

                // Console.WriteLine();
            }
            // Case 2: Within window but out of order
            else if (Between(
                frame.SequenceID,
                _frames[_lowerBound].SequenceID,
                _frames[_upperBound].SequenceID))
            {
                for (int i = 0; i < _frames.Length; i++)
                {
                    if (_frames[i].SequenceID == frame.SequenceID)
                    {
                        _frames[i] = frame;
                        break;
                    }
                }

                ack = new Frame
                {
                    SequenceID = _frames[_lowerBound].SequenceID,
                    Type = FrameType.NAK
                };
            }
            // Case 3: Otherwise inore     
        }
    }

}
