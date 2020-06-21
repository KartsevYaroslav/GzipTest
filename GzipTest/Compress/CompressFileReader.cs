using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using GzipTest.Infrastructure;
using GzipTest.Model;
using GzipTest.Processor;

namespace GzipTest.Compress
{
    internal class CompressFileReader : IProducer<Chunk>
    {
        private readonly int batchSize;
        private readonly string fileName;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly IBlockingCollection<Chunk> producingBag;
        private readonly Worker worker;

        public CompressFileReader(string fileName, int batchSize, IThreadPool threadPool, int concurrency)
        {
            worker = new Worker(threadPool);
            this.fileName = fileName;
            this.batchSize = batchSize;
            producingBag = new DisposableBlockingBag<Chunk>(concurrency);
            memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);
        }

        public IBlockingCollection<Chunk> StartProducing()
        {
            worker.Run(ReadFile);
            return producingBag;
        }

        public void Wait() => worker.Wait();

        public void Dispose() => memoryMappedFile.Dispose();

        private void ReadFile()
        {
            var fileInfo = new FileInfo(fileName);

            var offset = 0L;
            while (offset < fileInfo.Length)
            {
                var size = Math.Min(fileInfo.Length - offset, batchSize);
                var viewStream = memoryMappedFile.CreateViewStream(offset, size);
                var chunk = new Chunk(offset, viewStream);
                offset += viewStream.Length;
                producingBag.Add(chunk);
            }

            producingBag.CompleteAdding();
        }
    }
}