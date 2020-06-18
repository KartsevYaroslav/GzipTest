using System.Collections.Generic;
using System.IO;

namespace GzipTest.Decompress
{
    public class Decompressor : IProcessor
    {
        private readonly IProducer<Stream> producer;
        private readonly IConsumer<Chunk> consumer;
        private readonly uint concurrency;
        private readonly List<DecompressWorker> workers;
        private readonly BlockingBag<Chunk> chunkQueue;
        private BlockingBag<Stream>? queue;

        public Decompressor(IProducer<Stream> producer, IConsumer<Chunk> consumer, uint concurrency)
        {
            this.producer = producer;
            this.consumer = consumer;
            this.concurrency = concurrency;
            workers = new List<DecompressWorker>();
            chunkQueue = new BlockingBag<Chunk>(8);
        }

        public void Process()
        {
            Start();
            Wait();
        }

        private void Start()
        {
            queue = producer.StartProducing();
            consumer.StartConsuming(chunkQueue);
            for (var i = 0; i < concurrency; i++)
            {
                var worker = new DecompressWorker(queue, chunkQueue);
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

            chunkQueue.CompleteAdding();
            consumer.Wait();
        }

        public void Dispose()
        {
            producer.Dispose();
            consumer.Dispose();
            chunkQueue.Dispose();
            queue?.Dispose();
        }
    }
}