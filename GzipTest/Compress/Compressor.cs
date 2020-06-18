using System.Collections.Generic;
using System.IO;

namespace GzipTest.Compress
{
    public class Compressor : IProcessor
    {
        private readonly IProducer<Chunk> producer;
        private readonly IConsumer<Stream> consumer;
        private readonly uint concurrency;
        private readonly List<CompressWorker> workers;
        private readonly BlockingBag<Stream> streams;
        private BlockingBag<Chunk>? chunks;

        public Compressor(IProducer<Chunk> producer, IConsumer<Stream> consumer, uint concurrency)
        {
            this.producer = producer;
            this.consumer = consumer;
            this.concurrency = concurrency;
            workers = new List<CompressWorker>();
            streams = new BlockingBag<Stream>(8);
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
                var worker = new CompressWorker(chunks, streams);
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
            streams.Dispose();
            chunks?.Dispose();
        }
    }
}