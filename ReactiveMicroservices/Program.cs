using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveMicroservices
{
    class Program
    {
        private const int Iterations = 1000;

        private static int counter = 0;

        static void ClassicCombineTest()
        {
            var p1 = new Producer<ExampleEventArgs1>(15, () => new ExampleEventArgs1());
            var p2 = new Producer<ExampleEventArgs2>(40, () => new ExampleEventArgs2());
            Consumer c = new Consumer(p1, p2);

            c.OnCombinedEvents += IncreaseCounter;

            Parallel.Invoke(() => p1.Run(1000), () => p2.Run(500));
        }

        static void ReactiveCombineTest()
        {
            var p1 = new Producer<ExampleEventArgs1>(15, () => new ExampleEventArgs1());
            var p2 = new Producer<ExampleEventArgs2>(40, () => new ExampleEventArgs2());
            
            var p1obs = Observable.FromEvent<ExampleEventArgs1>(ev => p1.OnValueProduced += ev, ev => p1.OnValueProduced -= ev);
            var p2obs = Observable.FromEvent<ExampleEventArgs2>(ev => p2.OnValueProduced += ev, ev => p2.OnValueProduced -= ev);
            IDisposable sub = p1obs.CombineLatest(p2obs, (e1, e2) => new ExampleEventArgs3() { Event1 = e1, Event2 = e2 })
                                   .Subscribe(e => IncreaseCounter(e));

            Parallel.Invoke(() => p1.Run(1000), () => p2.Run(500));
            sub.Dispose();
        }

        static Tuple<long, long> RunTest(Action a, string testName)
        {
            counter = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            a();

            sw.Stop();
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine($"Test {testName} took {sw.ElapsedMilliseconds} ms, {counter} events has been generated");
            Console.WriteLine($"Consumed {proc.PrivateMemorySize64}");

            var result = Tuple.Create(sw.ElapsedMilliseconds, proc.PrivateMemorySize64);

            proc.Dispose();
            GC.Collect();

            return result;
        }

        static void ClassicBufferTest()
        {
            BufferExample<int> buff = new BufferExample<int>(1000, 100);
            Random rnd = new Random();

            Action<IEnumerable<int>> a = q =>
            {
                Interlocked.Add(ref counter, q.Count());
            };

            buff.BufferEvent += a;

            Parallel.Invoke(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    Task.Delay(15).Wait();
                    buff.Put(rnd.Next());
                }
            });

            buff.Stop();
        }

        static void ReactiveBufferTest()
        {
            var rnd = new Random();
            var producer = Observable.Generate(0, i => i < 1000, i => i + 1, i => rnd.Next(), i => TimeSpan.FromMilliseconds(15));

            IDisposable bufferedConsumer = producer.Buffer(TimeSpan.FromMilliseconds(1000), 100)
                                                   .Subscribe(list =>
                                                   {
                                                       Interlocked.Add(ref counter, list.Count);
                                                   });

            producer.Wait();

            bufferedConsumer.Dispose();
        }

        static void Main(string[] args)
        {
            var cc = RunTest(ClassicCombineTest, "ClassicCombine");
            var rc = RunTest(ReactiveCombineTest, "ReactiveCombine");
            var cb = RunTest(ClassicBufferTest, "Classic Buffer");
            var rb = RunTest(ReactiveBufferTest, "Reactive Buffer");

            Console.WriteLine($"Combine Time: {(double)cc.Item1 / (double)rc.Item1}");
            Console.WriteLine($"Combine Memory: {(double)cc.Item2 / (double)rc.Item2}");

            Console.WriteLine($"Buffer Time: {(double)cc.Item1 / (double)rc.Item1}");
            Console.WriteLine($"Buffer Memory: {(double)cb.Item2 / (double)rb.Item2}");

            Console.ReadKey();
        }

        private static void IncreaseCounter(ExampleEventArgs3 obj)
        {
            Interlocked.Increment(ref counter);
        }
    }
}
