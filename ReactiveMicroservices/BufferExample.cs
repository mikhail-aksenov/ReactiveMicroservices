using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;

namespace ReactiveMicroservices
{
    class BufferExample<T>
    {
        private BlockingCollection<T> _eventQueue;
        private readonly int _capacity;

        private Timer _timer;

        public event Action<IEnumerable<T>> BufferEvent;

        public BufferExample(double interval, int capacity)
        {
            this._capacity = capacity;
            this._eventQueue = new BlockingCollection<T>(_capacity);

            this._timer = new Timer(interval);
            this._timer.Elapsed += _timer_Elapsed;
            this._timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            EmitEvent();
        }

        public void Stop()
        {
            if (_eventQueue.Count != 0)
                EmitEvent();

            this._timer.Stop();
        }

        private void EmitEvent()
        {
            IList<T> buff = new List<T>();
            int count = _eventQueue.Count;
            int buffSize = count < _capacity ? count : _capacity;
            T val;
            for (int i = 0; i < buffSize && _eventQueue.TryTake(out val); i++)
                buff.Add(val);

            BufferEvent(buff);
        }


        public void Put(T element)
        {
            if (_eventQueue.Count == _capacity)
                EmitEvent();

            _eventQueue.Add(element);
        }
    }
}
