using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

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
        public long To => From + Length;
    }

    public class FileReader : IReader
    {
        private readonly string fileName;
        private readonly uint workersCount;
        private readonly BlockingCollection<Chunk> queue;
        private readonly List<FileReaderWorker> workers;

        public FileReader(string fileName, uint workersCount)
        {
            this.fileName = fileName;
            this.workersCount = workersCount;
            queue = new BlockingCollection<Chunk>();
            workers = new List<FileReaderWorker>();
        }

        public BlockingCollection<Chunk> StartReading()
        {
            var fileInfo = new FileInfo(fileName);

            var length = fileInfo.Length / workersCount;
            var start = 0L;
            const int batchSize = 1024 * 1024;
            for (var i = 0; i < workersCount - 1; i++)
            {
                var range = new Range(start, length);
                var readerWorker = new FileReaderWorker(fileName, range, batchSize, queue);
                readerWorker.Start();
                workers.Add(readerWorker);
                start = range.To;
            }

            var remainder = fileInfo.Length % workersCount;
            var infoLength = remainder == 0 ? fileInfo.Length - start : remainder;
            var lastRange = new Range(start, infoLength);
            var worker = new FileReaderWorker(fileName, lastRange, batchSize, queue);
            worker.Start();
            workers.Add(worker);

            return queue;
        }

        public void Wait()
        {
            foreach (var worker in workers)
            {
                worker.Wait();
            }

            queue.CompleteAdding();
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