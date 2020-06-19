using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using GzipTest.Gzip;
using GzipTest.Infrastructure;
using GzipTest.Model;

namespace GzipTest.Decompress
{
    public class DecompressFileWriter : IConsumer<Chunk>
    {
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly BlockingBag<byte[]> buffersPool;
        private volatile int count;
        private bool isDone;
        private readonly ManualResetEvent manualResetEvent;
        private readonly Worker worker;

        public DecompressFileWriter(IThreadPool threadPool, string fileName, long fileSize, uint concurrency)
        {
            buffersPool = new BlockingBag<byte[]>(concurrency);
            manualResetEvent = new ManualResetEvent(false);
            worker = new Worker(threadPool);

            for (var i = 0; i < concurrency; i++)
            {
                buffersPool.Add(new byte[1024 * 80]);
            }

            memoryMappedFile = MemoryMappedFile.CreateFromFile(
                fileName,
                FileMode.Open,
                null,
                fileSize,
                MemoryMappedFileAccess.ReadWrite
            );
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
                Interlocked.Increment(ref count);
                var viewStream = memoryMappedFile.CreateViewStream(
                    chunk.InitialOffset,
                    chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite
                );

                CopyStreamToStream(chunk.Content, viewStream, (x, y, z) =>
                {
                    x.Dispose();
                    y.Dispose();
                    Interlocked.Decrement(ref count);
                    if (isDone && count == 0)
                        manualResetEvent.Set();
                });
            }

            isDone = true;
        }

        private void CopyStreamToStream(
            Stream source, Stream destination,
            Action<Stream, Stream, Exception> completed)
        {
            buffersPool.TryTake(out var buffer);

            var read = source.Read(buffer);
            try
            {
                if (read > 0)
                {
                    destination.BeginWrite(buffer, 0, read, writeResult =>
                    {
                        try
                        {
                            destination.EndWrite(writeResult);
                            buffersPool.Add(buffer);
                            completed(source, destination, null);
                        }
                        catch (Exception exc)
                        {
                            completed(source, destination, exc);
                        }
                    }, null);
                }
                else
                {
                    completed(source, destination, null);
                }
            }
            catch (Exception exc)
            {
                completed(source, destination, exc);
            }
        }
    }
}