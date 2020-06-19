using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using GzipTest.Decompress;
using GzipTest.Gzip;
using GzipTest.Infrastructure;
using GzipTest.Model;

namespace GzipTest.Compress
{
    internal class CompressFileReader : IProducer<Chunk>
    {
        private readonly string fileName;
        private readonly IBlockingCollection<Chunk> producingBag;
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly Worker worker;

        public CompressFileReader(string fileName, IThreadPool threadPool, uint concurrency)
        {
            worker = new Worker(threadPool);
            this.fileName = fileName;
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

            const int batchSize = 1024 * 80;
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

        public void Dispose() => memoryMappedFile?.Dispose();
    }
}