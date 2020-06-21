using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace TP1
{
    public class A2_TimerManager
    {
        Timer[] _timers;

        public Action<int> OnFrameTimeoutHandler;

        public A2_TimerManager(int windowSize, int timeout)
        {
            _timers = new Timer[windowSize];
            for(int i = 0; i < windowSize; i++)
            {
                int idx = i;// needed for capture
                _timers[idx] = new Timer(timeout);
                _timers[idx].Elapsed += (object source, ElapsedEventArgs e) => OnFrameTimeoutHandler?.Invoke(idx);
            }        
        }

        public void Start(int idx)
        {
            if (idx < 0) return;
            if (idx >= _timers.Length) return;

            _timers[idx].Stop();
            _timers[idx].Start();           
        }

        public void Stop(int idx)
        {
            if (idx < 0) return;
            if (idx >= _timers.Length) return;

            _timers[idx].Stop();
        }
    }
}
