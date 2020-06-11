﻿using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTest
{
    public class CompressWorker
    {
        private readonly BlockingCollection<Chunk> chunkQueue;
        private readonly BlockingCollection<Stream> outQueue;
        private readonly Thread thread;

        public CompressWorker(BlockingCollection<Chunk> chunkQueue, BlockingCollection<Stream> outQueue)
        {
            this.chunkQueue = chunkQueue;
            this.outQueue = outQueue;
            thread = new Thread(Process);
        }

        public void Start() => thread.Start();

        private void Process()
        {
            var spinner = new SpinWait();
            while (true)
            {
                if (chunkQueue.IsAddingCompleted && chunkQueue.Count == 0)
                    break;

                if (!chunkQueue.TryTake(out var chunk))
                {
                    spinner.SpinOnce();
                    continue;
                }

                var memoryStream = new MemoryStream();
                using var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
                chunk.Content.CopyTo(gZipStream);
                gZipStream.Close();
                chunk.Content.Dispose();

                memoryStream.Position = 0;
                outQueue.Add(memoryStream);
            }
        }

        public void Wait() => thread.Join();
    }
}