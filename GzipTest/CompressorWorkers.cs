using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GzipTest
{
    public class CompressorWorkers : IDisposable
    {
        private readonly uint workersCount;
        private readonly List<CompressWorker> workers;
        private readonly IWriter writer;
        private readonly BlockingCollection<Chunk> queue;

        public CompressorWorkers(uint workersCount, BlockingCollection<Chunk> queue, IWriter writer)
        {
            this.workersCount = workersCount;
            this.queue = queue;
            this.writer = writer;
            workers = new List<CompressWorker>((int) workersCount);
        }

        public void Start()
        {
            for (var i = 0; i < workersCount; i++)
            {
                var worker = new CompressWorker(queue, writer);
                worker.Start();
                workers.Add(worker);
            }
        }

        public void WaitAll()
        {
            foreach (var worker in workers)
            {
                worker.Wait();
            }
        }

        public void Dispose()
        {
            queue.Dispose();
        }
    }
}