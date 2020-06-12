using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest
{
    public class DecompressWriter : IWriter<Chunk>
    {
        private readonly string fileName;
        private readonly long fileSize;
        private FileStream? fileStream;
        private BlockingCollection<Chunk>? queue;
        private Thread thread;

        public DecompressWriter(string fileName, long fileSize)
        {
            this.fileName = fileName;
            this.fileSize = fileSize;
            thread = new Thread(Write);
        }

        public void Start(BlockingCollection<Chunk> streams)
        {
            queue = streams;
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, fileSize,
                MemoryMappedFileAccess.ReadWrite);

            var spinWait = new SpinWait();
            var remainSize = 0L;
            while (true)
            {
                if (queue.IsAddingCompleted && queue.Count == 0)
                    break;

                if (!queue.TryTake(out var chunk))
                {
                    spinWait.SpinOnce();
                    continue;
                }

                spinWait = new SpinWait();

                using var viewStream = memoryMappedFile.CreateViewStream(chunk.InitialOffset, chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite);
                chunk.Content.CopyTo(viewStream);
                remainSize -= chunk.Content.Length;
                chunk.Content.Dispose();
            }

            if (remainSize > 0)
                throw new ArgumentException("Incorrect size");
        }

        public void Dispose()
        {
            fileStream?.Flush();
            fileStream?.Dispose();
        }
    }
}