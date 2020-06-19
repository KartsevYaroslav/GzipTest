using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest.Compress
{
    public class CompressFileReader : IProducer<Chunk>
    {
        private readonly string fileName;
        private readonly BlockingQueue<Chunk> queue;
        private readonly Thread thread;
        private readonly MemoryMappedFile memoryMappedFile;

        public CompressFileReader(string fileName)
        {
            this.fileName = fileName;
            queue = new BlockingQueue<Chunk>(8);
            memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            thread = new Thread(ReadFile);
        }

        public BlockingQueue<Chunk> StartProducing()
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

            const int batchSize = 1024 * 80;
            var offset = 0L;
            while (offset < fileInfo.Length)
            {
                var size = Math.Min(fileInfo.Length - offset, batchSize);
                var viewStream = memoryMappedFile.CreateViewStream(offset, size);
                var chunk = new Chunk(offset, viewStream);
                offset += viewStream.Length;
                queue.Add(chunk);
            }

            queue.CompleteAdding();
        }

        public void Dispose()
        {
            memoryMappedFile?.Dispose();
        }
    }
}