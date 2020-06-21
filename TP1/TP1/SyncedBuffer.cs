using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TP1
{
    public abstract class SyncedBuffer
    {
        public Mutex _mutex = new Mutex();

        public Action OnSentHandler;
        public Action OnReceivedHandler;


        // Empty: buffer is empty and ready to receive
        public virtual SemaphoreSlim Empty_ReadyToReceive { get; set; } = null;

        // Full: buffer is full
        //buffer hasnt been filled yet
        public virtual SemaphoreSlim Full_ReadyToSend { get; set; } = null;

        public virtual bool TryReceive(byte[] dest, int timeout=-1)
        {
            // Waiting until full
            OnReceivedHandler?.Invoke();

            if (Full_ReadyToSend.Wait(timeout))
            {
                _mutex.WaitOne();

                DoReceive(dest);

                _mutex.ReleaseMutex();
                Empty_ReadyToReceive.Release();

                return true;
            }

            return false;
        }

        public virtual bool TrySend(byte[] values, int timeout=-1)
        {
            // Waiting until empty
            if (Empty_ReadyToReceive.Wait(timeout))
            {
                _mutex.WaitOne();

                DoSend(values);

                _mutex.ReleaseMutex();
                Full_ReadyToSend.Release();

                OnSentHandler?.Invoke();
                return true;
            }

            return false;
        }

        public virtual void DoReceive(byte[] dest)
        {

        }

        public virtual void DoSend(byte[] dest)
        {

        }

    }


    // Utility for producer consumer problem
    public class SimpleSyncedBuffer : SyncedBuffer
    {
        public override SemaphoreSlim Empty_ReadyToReceive { get; set; } = new SemaphoreSlim(1, 1);

        // Full: buffer is full
        //buffer hasnt been filled yet
        public override SemaphoreSlim Full_ReadyToSend { get; set; } = new SemaphoreSlim(0, int.MaxValue);

        private ConcurrentQueue<byte[]> q = new ConcurrentQueue<byte[]>();

        //private byte[] _data;

        private int _itemSize;

        public SimpleSyncedBuffer(int itemSize)
        {
            //_data = new byte[size];
            _itemSize = itemSize;
        }

        // Infinite queue no need to buffer
        public override bool TrySend(byte[] values, int timeout = -1)
        {
            _mutex.WaitOne();

            DoSend(values);

            _mutex.ReleaseMutex();
            Full_ReadyToSend.Release();

            OnSentHandler?.Invoke();
            return true;        
        }


        public override bool TryReceive(byte[] dest, int timeout = -1)
        {
            // Waiting until full
            OnReceivedHandler?.Invoke();

            if (Full_ReadyToSend.Wait(timeout))
            {
                _mutex.WaitOne();

                DoReceive(dest);

                _mutex.ReleaseMutex();

                return true;
            }

            return false;
        }

        public override void DoReceive(byte[] dest)
        {
            byte[] bytes;
            if (q.TryDequeue(out bytes))
            {
                bytes.CopyTo(dest, 0);
            }
            else
            {
                // TODO assert false
                Console.WriteLine();
            }
        }

        public override void DoSend(byte[] values)
        {
            byte[] bytes = new byte[values.Length];
            values.CopyTo(bytes, 0);
            q.Enqueue(bytes);
        }
    }
}
