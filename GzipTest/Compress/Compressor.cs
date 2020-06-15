using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace GzipTest.Compress
{
    public class Compressor : IWorker
    {
        private readonly IReader<Chunk> reader;
        private readonly IWriter<Stream> writer;
        private readonly uint concurrency;
        private readonly List<CompressWorker> workers;
        private BoundedList<Stream> streams;

        public Compressor(IReader<Chunk> reader, IWriter<Stream> writer, uint concurrency)
        {
            this.reader = reader;
            this.writer = writer;
            this.concurrency = concurrency;
            workers = new List<CompressWorker>();
            streams = new BoundedList<Stream>(8);
        }

        public void Start()
        {
            var queue = reader.StartReading();
            writer.Start(streams);
            for (var i = 0; i < concurrency; i++)
            {
                var worker = new CompressWorker(queue, streams);
                worker.Start();
                workers.Add(worker);
            }
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
            // streams.Dispose();
            writer.Dispose();
            reader.Dispose();
        }
    }
}