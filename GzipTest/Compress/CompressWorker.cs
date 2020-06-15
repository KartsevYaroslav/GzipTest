using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTest.Compress
{
    public class CompressWorker
    {
        private readonly BoundedList<Chunk> chunkQueue;
        private readonly BoundedList<Stream> outQueue;
        private readonly Thread thread;

        public CompressWorker(BoundedList<Chunk> chunkQueue, BoundedList<Stream> outQueue)
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
                
                memoryStream.Position += 4;
                var offsetBytes = BitConverter.GetBytes(chunk.InitialOffset);
                memoryStream.Write(offsetBytes);

                using var gZipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true);
                chunk.Content.CopyTo(gZipStream);
                gZipStream.Close();
                chunk.Content.Dispose();

                memoryStream.Position = 0;
                var chunkSize = BitConverter.GetBytes((int) memoryStream.Length - 12);
                memoryStream.Write(chunkSize);
                memoryStream.Position = 0;

                outQueue.Add(memoryStream);
            }
        }

        public void Wait() => thread.Join();
    }
}