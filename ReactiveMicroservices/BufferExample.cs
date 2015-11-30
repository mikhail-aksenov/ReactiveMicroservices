using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ReactiveMicroservices
{
    class BufferExample<T>
    {
        private ConcurrentQueue<T> _q1;
        private ConcurrentQueue<T> _q2;

        private bool _q1active;

        private readonly int _capacity;
        private System.Timers.Timer _timer;

        private object _lock = new object();

        public event Action<IEnumerable<T>> BufferEvent;

        public BufferExample(double interval, int capacity)
        {
            this._capacity = capacity;
            this._q1 = new ConcurrentQueue<T>();
            this._q2 = new ConcurrentQueue<T>();
            this._q1active = true;

            this._timer = new System.Timers.Timer(interval);
            this._timer.Elapsed += _timer_Elapsed;
            this._timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SwitchBuffer();
        }

        public void Stop()
        {
            this._timer.Stop();
        }

        private void SwitchBuffer()
        {
            lock (_lock)
            {
                IList<T> buff = new List<T>();

                ConcurrentQueue<T> temp = _q1active ? _q1 : _q2;
            
                _q1active = !_q1active;

                T val;
                while (temp.TryDequeue(out val))
                {
                    buff.Add(val);
                }

                BufferEvent(buff);
            }
        }
        

        public void Put(T element)
        {
            ConcurrentQueue<T> q = _q1active ? _q1 : _q2;

            if (q.Count >= this._capacity)
                SwitchBuffer();

            lock (_lock)
                q.Enqueue(element);                
        }
    }
}
