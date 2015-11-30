using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMicroservices
{
    public class ExampleEventArgs1 : EventArgs
    {
        public Guid EventId { get; set; }

        public ExampleEventArgs1()
        {
            this.EventId = Guid.NewGuid();
        }
    }

    public class ExampleEventArgs2 : EventArgs
    {
        public Guid EventId { get; set; }

        public ExampleEventArgs2()
        {
            this.EventId = Guid.NewGuid();
        }
    }

    public class Producer<T> where T : EventArgs
    {
        private readonly int _timeout;
        private readonly Random _rnd;
        private readonly Func<T> _constr;

        public Producer(int timeout, Func<T> constr)
        {
            _timeout = timeout;
            _rnd = new Random();
            _constr = constr;
        }

        public event Action<T> OnValueProduced;

        public void CallProduce()
        {
            OnValueProduced(_constr());
        }

        public void Run(int iterations)
        {
            Type t = this.GetType();
            Console.WriteLine($"Ready to start {t}");
            
            for (int i = 0; i < iterations; i++)
            {
                Thread.Sleep(_timeout);
                OnValueProduced(_constr());
            }
        }
    }

    public class ExampleEventArgs3 : EventArgs
    {
        public ExampleEventArgs1 Event1 { get; set; }
        public ExampleEventArgs2 Event2 { get; set; }
    }

    public class Consumer
    {
        private ExampleEventArgs1 _e1;
        private ExampleEventArgs2 _e2;

        public Consumer(Producer<ExampleEventArgs1> p1, Producer<ExampleEventArgs2> p2)
        {
            p1.OnValueProduced += OnEvent1Received;
            p2.OnValueProduced += OnEvent2Received;
        }

        public event Action<ExampleEventArgs3> OnCombinedEvents;

        public void OnEvent1Received(ExampleEventArgs1 e)
        {
            this._e1 = e;
            if (_e2 != null)
                OnCombinedEvents(new ExampleEventArgs3() { Event1 = _e1, Event2 = _e2 });
        }

        public void OnEvent2Received(ExampleEventArgs2 e)
        {
            this._e2 = e;
            if (_e1 != null)
                OnCombinedEvents(new ExampleEventArgs3() { Event1 = _e1, Event2 = _e2 });
        }
    }
}
