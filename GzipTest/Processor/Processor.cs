using System;
using System.Collections.Generic;
using GzipTest.Infrastructure;

namespace GzipTest.Processor
{
    public class Processor<TIn, TOut> : IProcessor
        where TOut : IDisposable
        where TIn : IDisposable
    {
        private readonly IProducer<TIn> producer;
        private readonly IConsumer<TOut> consumer;
        private IBlockingCollection<TIn>? producingBag;
        private readonly IBlockingCollection<TOut> consumingBag;
        private readonly Func<TIn, TOut> mapper;
        private readonly IThreadPool threadPool;
        private readonly List<ITask> transformTasks;
        private readonly uint transformConcurrency;

        public Processor(
            IProducer<TIn> producer,
            IConsumer<TOut> consumer,
            IThreadPool threadPool,
            Func<TIn, TOut> mapper,
            uint transformConcurrency)
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

        public void Dispose()
        {
            producer.Dispose();
            consumer.Dispose();
            consumingBag.Dispose();
            threadPool.Dispose();
        }
    }
}