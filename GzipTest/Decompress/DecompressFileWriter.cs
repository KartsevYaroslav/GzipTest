using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest.Decompress
{
    public interface ITaskQueue
    {
        public void EnqueueTask(Action task);
        public void WaitAll();
    }

    class TaskQueue : ITaskQueue
    {
        private List<Thread> threads;
        private BlockingQueue<Action> actions;

        public TaskQueue(int workersCount)
        {
            threads = new List<Thread>();
            actions = new BlockingQueue<Action>((uint) workersCount);

            for (var i = 0; i < workersCount; i++)
            {
                var thread = new Thread(Execute);
                thread.Start();
                threads.Add(thread);
            }
        }

        private void Execute()
        {
            while (actions.TryTake(out var action))
            {
                action();
            }
        }

        public void EnqueueTask(Action task)
        {
            actions.Add(task);
        }

        public void WaitAll()
        {
            actions.CompleteAdding();
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
    }

    public class DecompressFileWriter : IConsumer<Chunk>
    {
        private readonly uint concurrency;
        private ITaskQueue queue;
        private MemoryMappedFile memoryMappedFile;
        private BlockingQueue<byte[]> buffersPool;
        private int count;
        private bool isDone;
        private ManualResetEvent manualResetEvent;

        public DecompressFileWriter(string fileName, long fileSize, uint concurrency)
        {
            this.concurrency = concurrency;
            buffersPool = new BlockingQueue<byte[]>(concurrency);
            manualResetEvent = new ManualResetEvent(false);

            for (var i = 0; i < concurrency; i++)
            {
                buffersPool.Add(new byte[1024 * 80]);
            }

            queue = new TaskQueue((int) 2);

            memoryMappedFile = MemoryMappedFile.CreateFromFile(
                fileName,
                FileMode.Open,
                null,
                fileSize,
                MemoryMappedFileAccess.ReadWrite
            );
        }

        public void StartConsuming(BlockingQueue<Chunk> consumingQueue) =>
            queue.EnqueueTask(() => Write(consumingQueue));

        public void Wait()
        {
            manualResetEvent.WaitOne();
            queue.WaitAll();
        }

        public void Dispose()
        {
            memoryMappedFile.Dispose();
        }

        private void Write(BlockingQueue<Chunk> chunks)
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
                    if (isDone && Interlocked.CompareExchange(ref count, 0, 0) == 0)
                        manualResetEvent.Set();
                });
            }

            isDone = true;
        }

        public void CopyStreamToStream(
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