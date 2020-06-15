using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GzipTest.Compress;

namespace GzipTest
{
    public class DecompressWorker
    {
        private readonly BoundedList<Chunk> chunkQueue;
        private readonly BoundedList<Stream> streamQueue;
        private readonly Thread thread;

        public DecompressWorker(BoundedList<Stream> streamQueue, BoundedList<Chunk> chunkQueue)
        {
            this.chunkQueue = chunkQueue;
            this.streamQueue = streamQueue;
            thread = new Thread(Process);
        }

        public void Start() => thread.Start();

        private void Process()
        {
            var spinner = new SpinWait();
            Span<byte> buffer = stackalloc byte[8];
            while (true)
            {
                if (streamQueue.IsAddingCompleted && streamQueue.Count == 0)
                    break;

                if (!streamQueue.TryTake(out var stream))
                {
                    spinner.SpinOnce();
                    continue;
                }

                spinner = new SpinWait();

                stream.Read(buffer);
                var initialOffset = BitConverter.ToInt64(buffer);

                var memoryStream = new MemoryStream(1024 * 80);
                using var gZipStream = new GZipStream(stream, CompressionMode.Decompress);
                gZipStream.CopyTo(memoryStream);
                stream.Dispose();
                gZipStream.Close();
                memoryStream.Position = 0;

                var chunk = new Chunk(initialOffset, memoryStream);

                chunkQueue.Add(chunk);
            }
        }

        public void Wait() => thread.Join();
    }
}