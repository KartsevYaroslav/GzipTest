using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using GzipTest.Gzip;
using GzipTest.Infrastructure;

namespace GzipTest.Decompress
{
    public class DecompressFileReader : IProducer<Stream>
    {
        private readonly string fileName;
        private readonly IBlockingCollection<Stream> bag;
        private readonly Worker worker;

        public DecompressFileReader(string fileName, IThreadPool threadPool, uint concurrency)
        {
            this.fileName = fileName;
            bag = new DisposableBlockingBag<Stream>(concurrency);
            worker = new Worker(threadPool);
        }

        public IBlockingCollection<Stream> StartProducing()
        {
            worker.Run(ReadFile);
            return bag;
        }


        public void Wait() => worker.Wait();

        private void ReadFile()
        {
            var fileInfo = new FileInfo(fileName);
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            var offset = 8L;

            Span<byte> sizeBuffer = stackalloc byte[4];
            while (offset < fileInfo.Length)
            {
                using (var tmpStream = memoryMappedFile.CreateViewStream(offset, 4))
                {
                    tmpStream.Read(sizeBuffer);
                }

                var size = BitConverter.ToInt32(sizeBuffer);
                var viewStream = memoryMappedFile.CreateViewStream(offset + 4, size + 8);
                offset += viewStream.Length + 4;
                bag.Add(viewStream);
            }

            bag.CompleteAdding();
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}