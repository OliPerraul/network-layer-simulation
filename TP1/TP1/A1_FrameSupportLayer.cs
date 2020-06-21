using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TP1
{
    public enum FrameSupport_EventType
    {
        Data,
        ACK // ACK/ NAK
    }

    public abstract class FrameSupportLayer : MachineLayer<FrameSupport_EventType>
    {
        protected FrameDataManipulationLayer _a2;
        protected TransmissionSupportLayer _c;

        //int msgSize = Frame.Size * 8;
        //512

        //2^r >= m + r +1        
        // 1024 >= 512 + 10 + 1
        // m = 512
        // r = 10
        public const int FrameSupportSize = 128;

        public FrameSupportLayer() : base() { }               

        public override SimpleSyncedBuffer UpTransferBuffer { get; set; } = new SimpleSyncedBuffer(Frame.Size);

        public virtual void Set(
            FrameDataManipulationLayer a2,
            TransmissionSupportLayer c)
        {
            _a2 = a2;
            _c = c;
        }

        public void OnACKSent()
        {
            _eventStream.Add(FrameSupport_EventType.ACK);
        }

        public void OnDataSent()
        {
            _eventStream.Add(FrameSupport_EventType.Data);
        }
    }

    public class A1_FrameSupportLayer : FrameSupportLayer
    {    
        public A1_FrameSupportLayer( ) : base() { }

        public override void Start()
        {
            base.Start();
        }

        public override void Set(FrameDataManipulationLayer a2, TransmissionSupportLayer c)
        {
            base.Set(a2, c);

            _a2.DownTransferBuffer.OnSentHandler += OnDataSent;
            _c.ACKOutputBuffer.OnSentHandler += OnACKSent;
        }    

        public override void Update()
        {
            var frameBytes = new byte[Frame.Size];
            var frameSupportBytes = new byte[FrameSupportSize];
            Frame frame;
            FrameSupport_EventType ev;
            while (true)
            {                
                frameBytes.Clear();

                switch (ev = WaitEvent())
                {
                    case FrameSupport_EventType.ACK:
                        _c.ReceiveACK(frameSupportBytes); 
                        Hamming.Decode(frameSupportBytes, frameBytes);
                        frame = Frame.FromBytes(frameBytes);
                        if(Parameters.Debug_C_A1) Console.WriteLine("[C -> A1] SequenceID: " + frame.SequenceID + ", Type: " + frame.Type);
                        UpTransferBuffer.TrySend(frameBytes);
                        break;

                    case FrameSupport_EventType.Data:
                        _a2.DownTransferBuffer.TryReceive(frameBytes);
                        frame = Frame.FromBytes(frameBytes);

                        if(Parameters.Debug_A1_C) Console.WriteLine("[A1 -> C] SequenceID: " + frame.SequenceID);

                        Hamming.Encode(frameBytes, frameSupportBytes);
                        _c.Send(frameSupportBytes);
                        break;
                }                               
            }
        }
    }

    public class B1_FrameSupport : FrameSupportLayer
    {
        public B1_FrameSupport() : base()
        {

        }

        public override void Set(FrameDataManipulationLayer a2, TransmissionSupportLayer c)
        {
            base.Set(a2, c);

            _a2.DownTransferBuffer.OnSentHandler += OnACKSent;
            _c.OutputBuffer.OnSentHandler += OnDataSent;
        }

        public override void Update()
        {
            var frameBytes = new byte[Frame.Size];
            var frameSupportBytes = new byte[FrameSupportSize];
            Frame frame;
            FrameSupport_EventType ev;
            while (true)
            {
                switch (ev = WaitEvent())
                {
                    case FrameSupport_EventType.Data:
                        _c.Receive(frameSupportBytes);
                        if(Parameters.Debug_C_B1) Console.WriteLine("[C -> B1, FrameSupport]");

                        int error = 0;

                        // Assume that only one error
                        if (Parameters.IsHammingCorrecting)
                        {
                            if ((error = Hamming.DetectError(frameSupportBytes)) != 0)
                            {
                                if(Parameters.Debug_B1_Corrected) Console.WriteLine("[B1, Single Bit Error Corrected] FrameNo: " + _c.FrameTransmittedCount);

                                // Flip the bit at error position
                                int byteIndex = (error - 1) / 8;
                                int bitIndex = (error - 1) % 8;

                                frameSupportBytes[byteIndex] ^= (byte)(1 << bitIndex);
                            }
                        }
                        // If detect an error, the frame is lost (do not send it above)
                        // There will be a missing frame in the window of the layer above
                        else if ((error = Hamming.DetectError(frameSupportBytes)) != 0)
                        {
                            if(Parameters.Debug_B1_Detected) Console.WriteLine("[B1, Detected] frameNo: " + _c.FrameTransmittedCount);
                            break;
                        }

                        Hamming.Decode(frameSupportBytes, frameBytes);
                        frame = Frame.FromBytes(frameBytes);                        
                        if(Parameters.Debug_B1_B2) Console.WriteLine("[B1 -> B2] SequenceID: " + frame.SequenceID + ", Type: " + frame.Type);
                        UpTransferBuffer.TrySend(frameBytes);
                        break;


                    case FrameSupport_EventType.ACK:
                        _a2.DownTransferBuffer.TryReceive(frameBytes);
                        frame = Frame.FromBytes(frameBytes);
                        if(Parameters.Debug_B1_C) Console.WriteLine("[B1 -> C] SequenceID: " + frame.SequenceID + ", Type: " + frame.Type);
                        Hamming.Encode(frameBytes, frameSupportBytes);
                        _c.SendACK(frameSupportBytes);
                        break;
                }
            }
        }
    }
}
