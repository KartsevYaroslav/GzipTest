using System.IO;
using System.IO.MemoryMappedFiles;
using GzipTest.Infrastructure;
using GzipTest.Processor;

namespace GzipTest.Decompress
{
    public class DecompressFileReader : IProducer<Stream>
    {
        private readonly string fileName;
        private readonly IBlockingCollection<Stream> bag;
        private readonly Worker worker;
        private readonly long fileHeaderSize;

        public DecompressFileReader(string fileName, long fileHeaderSize, IThreadPool threadPool, uint concurrency)
        {
            this.fileName = fileName;
            this.fileHeaderSize = fileHeaderSize;
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
            const int chunkLengthSize = sizeof(int);
            const int initialOffsetSize = sizeof(long);

            var fileInfo = new FileInfo(fileName);
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            var offset = fileHeaderSize;

            while (offset < fileInfo.Length)
            {
                using var tmpStream = memoryMappedFile.CreateViewStream(offset, chunkLengthSize);
                var chunkLength = tmpStream.ReadUInt32();

                var viewStream =
                    memoryMappedFile.CreateViewStream(offset + chunkLengthSize, chunkLength + initialOffsetSize);
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