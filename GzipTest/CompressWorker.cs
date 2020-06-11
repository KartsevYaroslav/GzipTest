using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTest
{
    public class CompressWorker
    {
        private readonly BlockingCollection<Chunk> chunkQueue;
        private readonly IWriter writer;
        private readonly Thread thread;

        public CompressWorker(BlockingCollection<Chunk> chunkQueue, IWriter writer)
        {
            this.chunkQueue = chunkQueue;
            this.writer = writer;
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

                using var memoryStream = new MemoryStream();
                using var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
                chunk.Content.CopyTo(gZipStream);
                gZipStream.Close();
                chunk.Content.Dispose();

                memoryStream.Position = 0;
                writer.Write(memoryStream);
            }
        }

        public void Wait() => thread.Join();
    }
}