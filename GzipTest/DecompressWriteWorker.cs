using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Threading;
using GzipTest.Compress;

namespace GzipTest
{
    public class DecompressWriteWorker
    {
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly long fileSize;
        private readonly BoundedList<Chunk> chunks;
        private readonly Thread thread;

        public DecompressWriteWorker(MemoryMappedFile memoryMappedFile, long fileSize, BoundedList<Chunk> chunks)
        {
            this.memoryMappedFile = memoryMappedFile;
            this.fileSize = fileSize;
            this.chunks = chunks;
            thread = new Thread(Write);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            var spinWait = new SpinWait();
            while (true)
            {
                if (chunks.IsAddingCompleted && chunks.Count == 0)
                    break;

                if (!chunks.TryTake(out var chunk))
                {
                    spinWait.SpinOnce();
                    continue;
                }

                spinWait = new SpinWait();

                using var viewStream = memoryMappedFile.CreateViewStream(chunk.InitialOffset, chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite);
                chunk.Content.CopyTo(viewStream);
                chunk.Content.Dispose();
                viewStream.Dispose();
            }
        }
    }
}