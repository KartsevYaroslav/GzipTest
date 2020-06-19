using System;
using System.Collections.Generic;

namespace GzipTest
{
    public class Processor<TIn, TOut> : IProcessor
        where TOut : IDisposable
        where TIn : IDisposable
    {
        private readonly IProducer<TIn> producer;
        private readonly IConsumer<TOut> consumer;
        private readonly uint concurrency;
        private readonly List<TransformWorker<TIn, TOut>> workers;
        private readonly BlockingQueue<TOut> streams;
        private BlockingQueue<TIn>? chunks;
        private readonly Func<TIn, TOut> mapper;

        public Processor(IProducer<TIn> producer, IConsumer<TOut> consumer, Func<TIn, TOut> mapper, uint concurrency)
        {
            this.producer = producer;
            this.consumer = consumer;
            this.concurrency = concurrency;
            this.mapper = mapper;
            workers = new List<TransformWorker<TIn, TOut>>();
            streams = new BlockingQueue<TOut>(concurrency);
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
                var worker = new TransformWorker<TIn, TOut>(chunks, streams, mapper);
                worker.Start();
                workers.Add(worker);
            }
        }

        private void Wait()
        {
            producer.Wait();
            foreach (var worker in workers)
            {
                worker.Wait();
            }

            streams.CompleteAdding();
            consumer.Wait();
        }

        public void Dispose()
        {
            producer.Dispose();
            consumer.Dispose();
            streams.Dispose();
            chunks?.Dispose();
        }
    }
}