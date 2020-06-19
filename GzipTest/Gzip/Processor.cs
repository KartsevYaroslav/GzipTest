using System;
using System.Collections.Generic;
using GzipTest.Infrastructure;

namespace GzipTest.Gzip
{
    public class Processor<TIn, TOut> : IProcessor
        where TOut : IDisposable
        where TIn : IDisposable
    {
        private readonly IProducer<TIn> producer;
        private readonly IConsumer<TOut> consumer;
        private readonly IBlockingCollection<TOut> streams;
        private IBlockingCollection<TIn>? chunks;
        private readonly Func<TIn, TOut> mapper;
        private readonly IThreadPool threadPool;
        private readonly List<ITask> tasks;
        private readonly uint concurrency;

        public Processor(
            IProducer<TIn> producer,
            IConsumer<TOut> consumer,
            IThreadPool threadPool,
            Func<TIn, TOut> mapper,
            uint concurrency)
        {
            this.producer = producer;
            this.consumer = consumer;
            this.concurrency = concurrency;
            this.threadPool = threadPool;
            this.mapper = mapper;
            streams = new DisposableBlockingBag<TOut>(concurrency);
            tasks = new List<ITask>();
        }

        public void Process()
        {
            Start();
            Wait();
        }

        private void Start()
        {
            chunks = producer.StartProducing();
            consumer.StartConsuming(streams);

            for (var i = 0; i < concurrency; i++)
            {
                var task = new Task(() =>
                {
                    while (chunks.TryTake(out var chunk))
                        streams.Add(mapper(chunk));
                });

                tasks.Add(task);
                threadPool.RunTask(task);
            }
        }

        private void Wait()
        {
            producer.Wait();
            threadPool.WaitAll(tasks);
            streams.CompleteAdding();
            consumer.Wait();
        }

        public void Dispose()
        {
            producer.Dispose();
            consumer.Dispose();
            streams.Dispose();
            threadPool.Dispose();
        }
    }
}