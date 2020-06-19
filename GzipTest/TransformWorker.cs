using System;
using System.Threading;

namespace GzipTest
{
    public class TransformWorker<TIn, TOut>
        where TIn : IDisposable
        where TOut : IDisposable
    {
        private readonly BlockingQueue<TIn> producingQueue;
        private readonly BlockingQueue<TOut> consumingQueue;
        private readonly Func<TIn, TOut> mapper;
        private readonly Thread thread;

        public TransformWorker(
            BlockingQueue<TIn> producingQueue,
            BlockingQueue<TOut> consumingQueue,
            Func<TIn, TOut> mapper)
        {
            this.producingQueue = producingQueue;
            this.consumingQueue = consumingQueue;
            this.mapper = mapper;
            thread = new Thread(Process);
        }

        public void Start() => thread.Start();

        private void Process()
        {
            while (producingQueue.TryTake(out var chunk))
                consumingQueue.Add(mapper(chunk));
        }

        public void Wait() => thread.Join();
    }
}