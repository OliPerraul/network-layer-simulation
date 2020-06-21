using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TP1
{
    public abstract class Layer<EventType>
    {
        protected Thread _thread;

        protected BlockingCollection<EventType> _eventStream = new BlockingCollection<EventType>();

        public EventType WaitEvent()
        {
            return _eventStream.Take();
        }


        public Layer()
        {
            _thread = new Thread(Update);
        }

        public virtual void Start()
        {
            _thread.Start();
        }

        public abstract void Update();
    }

    public abstract class MachineLayer<EventType> : Layer<EventType>
    {
        public virtual SimpleSyncedBuffer UpTransferBuffer { get; set; }

        public virtual SimpleSyncedBuffer DownTransferBuffer { get; set; }
        
        public MachineLayer() : base() { }        
    }
}

