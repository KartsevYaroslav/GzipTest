using System;
using System.Threading;

namespace GzipTest
{
    public class TransformWorker<TIn, TOut>
        where TIn : IDisposable
        where TOut : IDisposable
    {
        private readonly BlockingBag<TIn> producingBag;
        private readonly BlockingBag<TOut> consumingBag;
        private readonly Func<TIn, TOut> mapper;
        private readonly Thread thread;

        public TransformWorker(
            BlockingBag<TIn> producingBag,
            BlockingBag<TOut> consumingBag,
            Func<TIn, TOut> mapper)
        {
            this.producingBag = producingBag;
            this.consumingBag = consumingBag;
            this.mapper = mapper;
            thread = new Thread(Process);
        }

        public void Start() => thread.Start();

        private void Process()
        {
            while (producingBag.TryTake(out var chunk))
                consumingBag.Add(mapper(chunk));
        }

        public void Wait() => thread.Join();
    }
}