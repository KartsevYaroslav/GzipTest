using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest
{
    public interface IReader
    {
        BlockingCollection<Chunk> StartReading();
        void Wait();
    }

    public class Range
    {
        public Range(long @from, long length)
        {
            From = @from;
            Length = length;
        }

        public long From { get; }
        public long Length { get; }
    }

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
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            var offset = fileRange.From;
            while (offset < fileRange.Length)
            {
                var size = Math.Min(fileRange.Length - offset, batchSize);
                var viewStream = memoryMappedFile.CreateViewStream(offset, size);
                var chunk = new Chunk(offset, viewStream);
                queue.Add(chunk);
                offset += viewStream.Length;
            }

            queue.CompleteAdding();
        }
    }

    public class FileReader : IReader
    {
        private readonly string fileName;
        private readonly uint workersCount;
        private readonly BlockingCollection<Chunk> queue;
        private List<FileReaderWorker> workers;
        private Thread thread;

        public FileReader(string fileName, uint workersCount)
        {
            this.fileName = fileName;
            this.workersCount = workersCount;
            queue = new BlockingCollection<Chunk>();
            thread = new Thread(ReadFile);
        }

        public BlockingCollection<Chunk> StartReading()
        {
            thread.Start();
            return queue;
        }

        public void Wait()
        {
            thread.Join();
        }
        //
        // private void ReadFile()
        // {
        //     using var fileStream = File.OpenRead(fileName);
        //
        //     const int batchSize = 1_048_576;
        //     var bytesToRead = fileStream.Length;
        //     while (bytesToRead > 0)
        //     {
        //         Memory<byte> buffer = new byte[batchSize];
        //         var offset = fileStream.Position;
        //         var readBytes = fileStream.Read(buffer.Span);
        //         var chunk = new Chunk(offset, buffer.Slice(0, readBytes));
        //         bytesToRead -= readBytes;
        //
        //         queue.Add(chunk);
        //     }
        //
        //     queue.CompleteAdding();
        // }

        private void ReadFile()
        {
            var fileInfo = new FileInfo(fileName);

            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            const int batchSize = 1_048_576;
            var offset = 0L;
            while (offset < fileInfo.Length)
            {
                var size = Math.Min(fileInfo.Length - offset, batchSize);
                var viewStream = memoryMappedFile.CreateViewStream(offset, size);
                var chunk = new Chunk(offset, viewStream);
                queue.Add(chunk);
                offset += viewStream.Length;
            }

            queue.CompleteAdding();
        }
    }
}