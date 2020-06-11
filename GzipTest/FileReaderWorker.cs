using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest
{
    public class FileReaderWorker
    {
        private readonly string fileName;
        private readonly Range fileRange;
        private readonly BlockingCollection<Chunk> queue;
        private readonly Thread thread;
        private readonly int batchSize;

        public FileReaderWorker(string fileName, Range fileRange, int batchSize, BlockingCollection<Chunk> queue)
        {
            this.fileName = fileName;
            this.fileRange = fileRange;
            this.queue = queue;
            this.batchSize = batchSize;

            thread = new Thread(ReadFile);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void ReadFile()
        {
            using var memoryMappedFile =
                MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

            var offset = fileRange.From;
            while (offset < fileRange.To)
            {
                var size = Math.Min(fileRange.To - offset, batchSize);
                var viewStream = memoryMappedFile.CreateViewStream(offset, size, MemoryMappedFileAccess.Read);
                var chunk = new Chunk(offset, viewStream);
                queue.Add(chunk);
                offset += viewStream.Length;
            }
        }
    }
}