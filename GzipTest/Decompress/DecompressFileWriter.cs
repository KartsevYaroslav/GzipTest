using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using GzipTest.Infrastructure;
using GzipTest.Model;
using GzipTest.Processor;

namespace GzipTest.Decompress
{
    public class DecompressFileWriter : IConsumer<Chunk>
    {
        private readonly BlockingBag<byte[]> buffersPool;
        private readonly ManualResetEvent manualResetEvent;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly Worker worker;
        private bool isDone;
        private volatile int writtenChunks;

        public DecompressFileWriter(
            IThreadPool threadPool,
            string fileName,
            long fileSize,
            int concurrency
        )
        {
            manualResetEvent = new ManualResetEvent(false);
            worker = new Worker(threadPool);
            memoryMappedFile = MemoryMappedFile.CreateFromFile(
                fileName,
                FileMode.Open,
                null,
                fileSize,
                MemoryMappedFileAccess.ReadWrite
            );

            buffersPool = new BlockingBag<byte[]>(concurrency);
            for (var i = 0; i < concurrency; i++)
                buffersPool.Add(new byte[1024 * 80]);
        }

        public void StartConsuming(IBlockingCollection<Chunk> consumingBag) => worker.Run(() => Write(consumingBag));

        public void Wait()
        {
            worker.Wait();
            manualResetEvent.WaitOne();
        }

        public void Dispose()
        {
            buffersPool.Dispose();
            memoryMappedFile.Dispose();
        }

        private void Write(IBlockingCollection<Chunk> chunks)
        {
            while (chunks.TryTake(out var chunk))
            {
                Interlocked.Increment(ref writtenChunks);
                var viewStream = memoryMappedFile.CreateViewStream(
                    chunk.InitialOffset,
                    chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite
                );

                chunk.Content.CopyToAsync(viewStream, buffersPool, OnComplete);
            }

            isDone = true;

            void OnComplete(Stream source, Stream target)
            {
                source.Dispose();
                target.Dispose();
                Interlocked.Decrement(ref writtenChunks);
                if (isDone && writtenChunks == 0)
                    manualResetEvent.Set();
            }
        }
    }
}