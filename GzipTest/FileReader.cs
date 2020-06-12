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
        public long To => From + Length;
    }

    public class FileReader : IReader
    {
        private readonly string fileName;
        private readonly BlockingCollection<Chunk> queue;
        private readonly Thread thread;

        public FileReader(string fileName)
        {
            this.fileName = fileName;
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

        private void ReadFile()
        {
            var fileInfo = new FileInfo(fileName);

            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            const int batchSize = 1024 * 1024;
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