using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace GzipTest
{
    public class Compressor : ICompressor, IDisposable
    {
        private readonly IReader reader;
        private readonly IWriter writer;
        private readonly uint concurrency;
        private readonly List<CompressWorker> workers;
        private BlockingCollection<Stream> streams;

        public Compressor(IReader reader, IWriter writer, uint concurrency)
        {
            this.reader = reader;
            this.writer = writer;
            this.concurrency = concurrency;
            workers = new List<CompressWorker>();
        }

        public void Start()
        {
            var queue = reader.StartReading();
            streams = new BlockingCollection<Stream>();
            for (var i = 0; i < concurrency; i++)
            {
                var worker = new CompressWorker(queue, streams);
                worker.Start();
                workers.Add(worker);
            }

            writer.Start(streams);
        }

        public void Wait()
        {
            reader.Wait();
            foreach (var worker in workers)
            {
                worker.Wait();
            }

            streams.CompleteAdding();
            writer.Wait();
        }

        public void Dispose()
        {
            streams.Dispose();
        }
    }
}