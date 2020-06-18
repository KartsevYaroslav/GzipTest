using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTest.Decompress
{
    public class DecompressWorker
    {
        private readonly BlockingBag<Chunk> chunkQueue;
        private readonly BlockingBag<Stream> streamQueue;
        private readonly Thread thread;

        public DecompressWorker(BlockingBag<Stream> streamQueue, BlockingBag<Chunk> chunkQueue)
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