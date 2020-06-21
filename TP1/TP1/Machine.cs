using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Queue = System.Collections.Concurrent.ConcurrentQueue<byte[]>;


namespace TP1
{
    public class Machine
    {
        protected UserDataLayer _a3;
        protected FrameDataManipulationLayer _a2;
        protected FrameSupportLayer _a1;
        protected TransmissionSupportLayer _c;

        private bool _isSender = false;       

        public Machine(
            TransmissionSupportLayer c,
            bool isSender)
        {
            _isSender = isSender;

            _c = c;
            _a3 = _isSender ?
                (UserDataLayer)new A3_UserDataLayer() :
                (UserDataLayer)new B3_UserData();

            _a2 = _isSender ?
                (FrameDataManipulationLayer) new A2_FrameDataManipulationLayer() :
                (FrameDataManipulationLayer) new B2_FrameDataManipulation();

            _a1 = _isSender ?
                (FrameSupportLayer)new A1_FrameSupportLayer() :
                (FrameSupportLayer)new B1_FrameSupport();

            _a3.Set(_a2);
            _a2.Set(_a1, _a3);
            _a1.Set(_a2, c);
        }

        public void Start()
        {
            _a3.Start();
            _a2.Start();
            _a1.Start();
        }
    }
}
