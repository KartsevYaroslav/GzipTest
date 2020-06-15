using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using GzipTest.Compress;

namespace GzipTest
{
    public class Decompressor : IWorker, IDisposable
    {
        private readonly IReader<Stream> reader;
        private readonly IWriter<Chunk> writer;
        private readonly uint concurrency;
        private readonly List<DecompressWorker> workers;
        private readonly BoundedList<Chunk> chunkQueue;

        public Decompressor(IReader<Stream> reader, IWriter<Chunk> writer, uint concurrency)
        {
            this.reader = reader;
            this.writer = writer;
            this.concurrency = concurrency;
            workers = new List<DecompressWorker>();
            chunkQueue = new BoundedList<Chunk>(8);
        }

        public void Start()
        {
            var queue = reader.StartReading();
            writer.Start(chunkQueue);
            for (var i = 0; i < concurrency; i++)
            {
                var worker = new DecompressWorker(queue, chunkQueue);
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

            chunkQueue.CompleteAdding();
            writer.Wait();
        }

        public void Dispose()
        {
            // chunkQueue.Dispose();
            reader.Dispose();
            writer.Dispose();
        }
    }
}