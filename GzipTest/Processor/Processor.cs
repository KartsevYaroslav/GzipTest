using System;
using System.Collections.Generic;
using GzipTest.Infrastructure;

namespace GzipTest.Processor
{
    public class Processor<TIn, TOut> : IProcessor
        where TOut : IDisposable
        where TIn : IDisposable
    {
        private readonly IConsumer<TOut> consumer;
        private readonly IBlockingCollection<TOut> consumingBag;
        private readonly Func<TIn, TOut> mapper;
        private readonly IProducer<TIn> producer;
        private readonly IThreadPool threadPool;
        private readonly int transformConcurrency;
        private readonly List<ITask> transformTasks;
        private IBlockingCollection<TIn>? producingBag;

        public Processor(
            IProducer<TIn> producer,
            IConsumer<TOut> consumer,
            IThreadPool threadPool,
            Func<TIn, TOut> mapper,
            int transformConcurrency)
        {
            this.producer = producer;
            this.consumer = consumer;
            this.transformConcurrency = transformConcurrency;
            this.threadPool = threadPool;
            this.mapper = mapper;
            consumingBag = new DisposableBlockingBag<TOut>(transformConcurrency);
            transformTasks = new List<ITask>();
        }

        public void Process()
        {
            Start();
            Wait();
        }

        public void Dispose()
        {
            producer.Dispose();
            consumer.Dispose();
            consumingBag.Dispose();
            threadPool.Dispose();
        }

        private void Start()
        {
            producingBag = producer.StartProducing();
            consumer.StartConsuming(consumingBag);

            for (var i = 0; i < transformConcurrency; i++)
            {
                var task = new Task(() =>
                {
                    while (producingBag.TryTake(out var chunk))
                        consumingBag.Add(mapper(chunk));
                });

                transformTasks.Add(task);
                threadPool.RunTask(task);
            }
        }

        private void Wait()
        {
            producer.Wait();
            threadPool.WaitAll(transformTasks);
            consumingBag.CompleteAdding();
            consumer.Wait();
        }
    }
}