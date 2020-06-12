using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTest
{
    public class DecompressWorker
    {
        private readonly BlockingCollection<Chunk> chunkQueue;
        private readonly BlockingCollection<Stream> streamQueue;
        private readonly Thread thread;

        public DecompressWorker(BlockingCollection<Stream> streamQueue, BlockingCollection<Chunk> chunkQueue)
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

                if (!streamQueue.TryTake(out var stream) || chunkQueue.Count > 10)
                {
                    spinner.SpinOnce();
                    continue;
                }
                
                spinner = new SpinWait();
                
                stream.Read(buffer);
                var initialOffset = BitConverter.ToInt64(buffer);

                var memoryStream = new MemoryStream();
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