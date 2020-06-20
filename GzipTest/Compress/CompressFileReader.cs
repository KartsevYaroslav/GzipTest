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
        private readonly string fileName;
        private readonly IBlockingCollection<Chunk> producingBag;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly Worker worker;
        private readonly uint batchSize;

        public CompressFileReader(string fileName, uint batchSize, IThreadPool threadPool, uint concurrency)
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

        public void Dispose() => memoryMappedFile.Dispose();
    }
}