using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TP1
{
    public enum FrameDataManipulation_EventType
    {
        UserData,
        TimeOut,
        ACK, // ACK/ NAK
        Frame // ACK/ NAK
    }

    public struct FrameDataManipulation_Event
    {
        public FrameDataManipulation_EventType Type;
        public int TimeOut_WindowFrameIndex;
        //public Frame ArrivalNotification_Frame;
    }

    public abstract class FrameDataManipulationLayer : MachineLayer<FrameDataManipulation_Event>
    {
        protected FrameSupportLayer _a1;

        protected UserDataLayer _a3;

        public FrameDataManipulationLayer() : base()
        {
        }

        public virtual void Set(FrameSupportLayer a1, UserDataLayer a3)
        {
            _a1 = a1;
            _a3 = a3;
        }
    }

    public class A2_FrameDataManipulationLayer : FrameDataManipulationLayer
    {
        private bool _running = true;

        private int _windowSize = 0;

        private int _timeout = 0;

        private A2_TimerManager _timerManager;

        private A_SenderWindow _window;

        public override SimpleSyncedBuffer UpTransferBuffer { get; set; } = new SimpleSyncedBuffer(UserData.Size);

        public override SimpleSyncedBuffer DownTransferBuffer { get; set; } = new SimpleSyncedBuffer(Frame.Size);

        public A2_FrameDataManipulationLayer() : base()
        {
            _windowSize = Parameters.WinSizeA;
            _timeout = Parameters.TimeOutA;
            _window = new A_SenderWindow(_windowSize);
            _timerManager = new A2_TimerManager(_windowSize, _timeout);
            _timerManager.OnFrameTimeoutHandler += OnFrameTimeout;
        }

        public override void Set(FrameSupportLayer a1, UserDataLayer a3)
        {
            base.Set(a1, a3);
            // Data channel
            a3.DownTransferBuffer.OnSentHandler += OnDataSent;
            // ACK channel
            a1.UpTransferBuffer.OnSentHandler += OnACKSent;
        }

        public void OnDataSent()
        {
            _eventStream.Add(new FrameDataManipulation_Event
            {
                Type = FrameDataManipulation_EventType.UserData,
            });
        }

        public void OnACKSent()
        {
            _eventStream.Add(new FrameDataManipulation_Event
            {
                Type = FrameDataManipulation_EventType.ACK,
            });
        }

        public void OnFrameTimeout(int frameIdx)
        {
            _eventStream.Add(new FrameDataManipulation_Event
            {
                Type = FrameDataManipulation_EventType.TimeOut,
                TimeOut_WindowFrameIndex = frameIdx
            });
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            var ackframeBytes = new byte[Frame.Size];
            var ackFrame = new Frame { };

            var userDataBytes = new byte[UserData.Size];
            var userData = new UserData
            {
                OccupiedSize = 0,
                IsEOF = false,
                Content = new byte[UserData.ContentSize]
            };

            var frameBytes = new byte[Frame.Size];
            var userDataframe = new Frame
            {
                SequenceID = 0,
                Content = new byte[Frame.ContentSize],
                OccupiedSize = 0,
                Type = FrameType.Data
            };

            var retransmitFrame = new Frame
            {
                SequenceID = 0,
                Content = new byte[Frame.ContentSize],
                OccupiedSize = 0,
                Type = FrameType.Data
            };

            FrameDataManipulation_Event ev;           

            int windowFrameIndex;

            while (_running)
            {
                ev = WaitEvent();

                // Block here and wait for an event                
                switch (ev.Type)
                {                                    
                    case FrameDataManipulation_EventType.ACK:
                        _a1.UpTransferBuffer.TryReceive(ackframeBytes);
                        ackFrame = Frame.FromBytes(ackframeBytes);
                        if(Parameters.Debug_A1_A2) Console.WriteLine("[A1 -> A2] SequenceID: " + ackFrame.SequenceID + ", Type: " + ackFrame.Type);
                        switch (ackFrame.Type)
                        {
                            case FrameType.ACK:                                
                                _window.Update(ackFrame, _timerManager);                         
                                break;
                            
                            // Last frame was received
                            case FrameType.ACK_EndOfTransmission:
                                _window.Update(ackFrame, _timerManager);
                                _running = false;
                                break;

                            // Retransmit only required frame
                            case FrameType.NAK:
                                retransmitFrame = _window.GetBySequenceId(ackFrame.SequenceID);
                                if (retransmitFrame.Type == FrameType.None)
                                {
                                    // TODO: Assert never the case..
                                    _eventStream.Add(ev);
                                    break;
                                }

                                if(Parameters.Debug_A2_Retransmitted) Console.WriteLine("[A2, Retransmitted] SequenceID: " + retransmitFrame.SequenceID);
                                DownTransferBuffer.TrySend(retransmitFrame.ToBytes());

                                break;
                        }

                        break;

                    case FrameDataManipulation_EventType.TimeOut:
                        // Timeout frame content may not be copied correctly

                        if(Parameters.Debug_A2_Timeout) Console.WriteLine("[A2, Timeout] SequenceID: " + retransmitFrame.SequenceID);

                        retransmitFrame = _window.GetByIndex(ev.TimeOut_WindowFrameIndex);
                        DownTransferBuffer.TrySend(retransmitFrame.ToBytes());

                        //evt.TimeOut_FrameIndex
                        break;

                    case FrameDataManipulation_EventType.UserData:

                        // Check if enough room in the window
                        if (_window.Full)
                        {
                            // Put the event back in the queue
                            _eventStream.Add(ev);
                            break;
                        }

                        // Wait for full
                        Array.Clear(userDataBytes, 0, userDataBytes.Length);
                        _a3.DownTransferBuffer.TryReceive(userDataBytes);
                        userData = UserData.FromBytes(userDataBytes);

                        if(Parameters.Debug_A3_A2) Console.WriteLine("[A3 -> A2, UserData]");

                        userData.Content
                            .SubArray(0, userData.OccupiedSize)
                            .CopyTo(userDataframe.Content, userDataframe.OccupiedSize);

                        userDataframe.OccupiedSize += userData.OccupiedSize;

                        // If finished reading we have set the first byte to the amount read
                        // mark as end of transmission
                        userDataframe.Type = userData.OccupiedSize < UserData.ContentSize ?
                            FrameType.EndOfTransmission :
                            FrameType.Data;

                        // If frame filled we send it (Or store it in the window)
                        if (userDataframe.OccupiedSize + UserData.ContentSize >= Frame.ContentSize ||
                            userDataframe.Type == FrameType.EndOfTransmission)
                        {                        
                            windowFrameIndex = _window.Store(userDataframe);
                            _timerManager.Start(windowFrameIndex);

                            if(Parameters.Debug_A2_A1) Console.WriteLine("[A2 -> A1] SequenceID: " + userDataframe.SequenceID + ", Type: " + userDataframe.Type);
                            DownTransferBuffer.TrySend(userDataframe.ToBytes());

                            Array.Clear(userDataframe.Content, 0, userDataframe.Content.Length);
                            userDataframe.SequenceID = (userDataframe.SequenceID + 1) % Parameters.MaxSequenceID;
                            userDataframe.OccupiedSize = 0;
                        }

                        break;
                }                
            }


        }
    }


    public class B2_FrameDataManipulation : FrameDataManipulationLayer
    {
        private int _windowSize = 0;

        private int _timeout = 0;

        private B_ReceiverWindow _window;

        public override SimpleSyncedBuffer UpTransferBuffer { get; set; } = new SimpleSyncedBuffer(UserData.Size);

        public override SimpleSyncedBuffer DownTransferBuffer { get; set; } = new SimpleSyncedBuffer(Frame.Size);

        // TODO Parameters.Delay?
        private int _ackTimeout = 10;

        public B2_FrameDataManipulation() : base()
        {
            _windowSize = Parameters.WinSizeB;
            _timeout = Parameters.TimeOutB;
            _window = new B_ReceiverWindow(_windowSize);
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Set(FrameSupportLayer a1, UserDataLayer a3)
        {
            base.Set(a1, a3);
        }

        public override void Update()
        {
            var frameBytes = new byte[Frame.Size];
            var frame = new Frame
            {
                SequenceID = 0,
                Content = new byte[Frame.ContentSize],
                OccupiedSize = 0,
                Type = FrameType.None
            };
            var ackFrame = new Frame
            {
                SequenceID = 0,
                Content = new byte[Frame.ContentSize],
                OccupiedSize = 0,
                Type = FrameType.ACK
            };

            var userDataBytes = new byte[UserData.Size];
            var userData = new UserData
            {
                OccupiedSize = 0,
                IsEOF = false,
                Content = new byte[UserData.ContentSize]
            };

            List<Frame> readyToSendUp = new List<Frame>();

            while (true)
            {
                Array.Clear(frameBytes, 0, frameBytes.Length);
                _a1.UpTransferBuffer.TryReceive(frameBytes);

                frame = Frame.FromBytes(frameBytes);
                _window.Update(frame, out ackFrame, out readyToSendUp);
                if (ackFrame.Type == FrameType.None)
                {
                    _window.Update(frame, out ackFrame, out readyToSendUp);
                    continue; // frame out of window, we drop it..
                }

                DownTransferBuffer.TrySend(ackFrame.ToBytes());
                if(Parameters.Debug_B2_B1) Console.WriteLine("[B2 -> B1] SequenceID: " + ackFrame.SequenceID + ", Type: " + ackFrame.Type);

                // Send all the frames that are readytosend up in order
                foreach (var readyFrame in readyToSendUp)
                {
                    // Get next frames
                    int currentFrameProgress = 0;
                    while (currentFrameProgress < readyFrame.OccupiedSize)
                    {
                        // If last userdata of lastframe, then user data is not full ocuppied
                        userData.OccupiedSize = (byte)
                        ((currentFrameProgress + UserData.ContentSize >= readyFrame.OccupiedSize &&
                        readyFrame.Type == FrameType.EndOfTransmission) ?
                            readyFrame.OccupiedSize - currentFrameProgress :
                            UserData.ContentSize);

                        Array.Clear(userData.Content, 0, userData.Content.Length);
                        readyFrame.Content
                            .SubArray(
                            currentFrameProgress,
                            userData.OccupiedSize)
                            .CopyTo(userData.Content, 0);

                        userData.IsEOF =
                                currentFrameProgress + UserData.ContentSize >= readyFrame.OccupiedSize &&
                                frame.Type == FrameType.EndOfTransmission;

                        if(Parameters.Debug_B2_B3) Console.WriteLine("[B2 -> B3]");
                        UpTransferBuffer.TrySend(userData.ToBytes());

                        currentFrameProgress += UserData.ContentSize;

                        if (userData.IsEOF) break;
                    }
                }
            }
        }
    }
}
