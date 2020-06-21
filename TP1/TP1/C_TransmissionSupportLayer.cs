using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TP1
{
    public enum TransmissionSupport_EventType
    {
        ACK,
        Data        
    }

    public class TransmissionSupportLayer : Layer<TransmissionSupport_EventType>
    {
        public SimpleSyncedBuffer InputBuffer { get; set; }
        public SimpleSyncedBuffer OutputBuffer { get; set; }
    
        public SimpleSyncedBuffer ACKInputBuffer { get; set; }
        public SimpleSyncedBuffer ACKOutputBuffer { get; set; }

        public const int ErrorChance = 50;

        private long _frameTransmittedCount = 0;
        public long FrameTransmittedCount => _frameTransmittedCount;

        public TransmissionSupportLayer( ) : base()
        {
            InputBuffer = new SimpleSyncedBuffer(FrameSupportLayer.FrameSupportSize);
            OutputBuffer = new SimpleSyncedBuffer(FrameSupportLayer.FrameSupportSize);        
            ACKInputBuffer = new SimpleSyncedBuffer(FrameSupportLayer.FrameSupportSize);
            ACKOutputBuffer = new SimpleSyncedBuffer(FrameSupportLayer.FrameSupportSize);            
        
            InputBuffer.OnSentHandler += OnDataSent;
            ACKInputBuffer.OnSentHandler += OnACKSent;
        }

        public void OnDataSent()
        {
            _eventStream.Add(TransmissionSupport_EventType.Data);
        }

        public void OnACKSent()
        {
            _eventStream.Add(TransmissionSupport_EventType.ACK);
        }

        public void Send(byte[] values)
        {
            InputBuffer.TrySend(values);
        }

        public void Receive(byte[] dest)
        {
            OutputBuffer.TryReceive(dest);
        }

        public void SendACK(byte[] values)
        {
            ACKInputBuffer.TrySend(values);
        }

        public void ReceiveACK(byte[] dest)
        {
            ACKOutputBuffer.TryReceive(dest);
        }
   

        public override void Start()
        {
            base.Start();
        }

        // TODO: Not simulated anymore .. rename, remove? ..
        public void IntroduceError(byte[] frameSupportBytes)
        {
            foreach (int i in Parameters.BitErrorPositions)
            {
                int j = i / 8;
                frameSupportBytes[j] = (byte)(frameSupportBytes[j] ^ (1 << (i % 8)));
            }
        }

        public override void Update()
        {
            var frameSupportBytes = new byte[FrameSupportLayer.FrameSupportSize];
            var frameBytes = new byte[Frame.Size];
            TransmissionSupport_EventType ev;
            while (true)
            {
                // Simulate support latency
                Thread.Sleep(Parameters.ThreadBufferDelay);

                switch (ev = WaitEvent())
                {
                    case TransmissionSupport_EventType.ACK:
                        ACKInputBuffer.TryReceive(frameSupportBytes);
                        ACKOutputBuffer.TrySend(frameSupportBytes);
                        break;

                    case TransmissionSupport_EventType.Data:

                        Array.Clear(frameSupportBytes, 0, frameSupportBytes.Length);
                        InputBuffer.TryReceive(frameSupportBytes);

                        Hamming.Decode(frameSupportBytes, frameBytes);
                        var fr = Frame.FromBytes(frameBytes);

                        // Simulate errors
                        switch (Parameters.ErrorChangeType)
                        {
                            case ErrorChangeType.A_AllFrames:
                                if(Parameters.Debug_C_Error) Console.WriteLine("[C, Make Error] frameNo: " + FrameTransmittedCount);
                                IntroduceError(frameSupportBytes);
                                break;

                            case ErrorChangeType.B_RandomFrames:
                                int percent = new Random((int)DateTime.UtcNow.Ticks).Next(0, 100);
                                if (percent < ErrorChance)
                                {
                                    if(Parameters.Debug_C_Error) Console.WriteLine("[C, Make Error] frameNo: " + FrameTransmittedCount);
                                    IntroduceError(frameSupportBytes);
                                }
                                break;

                            case ErrorChangeType.C_SpecifiedFrames:
                                foreach (var errorFrameId in Parameters.FramesToChange)
                                {
                                    if (errorFrameId == _frameTransmittedCount)
                                    {
                                        if(Parameters.Debug_C_Error) Console.WriteLine("[C, Make Error] frameNo: " + FrameTransmittedCount);
                                        IntroduceError(frameSupportBytes);
                                        break;
                                    }
                                }

                                break;
                            case ErrorChangeType.D_NoError:
                                break;
                        }

                        _frameTransmittedCount++;
                        OutputBuffer.TrySend(frameSupportBytes);
                        break;
                }
            }
        }
    }
}
