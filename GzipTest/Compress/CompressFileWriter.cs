using System.IO;
using GzipTest.Infrastructure;
using GzipTest.Processor;

namespace GzipTest.Compress
{
    public class CompressFileWriter : IConsumer<Stream>
    {
        private readonly string fileName;
        private readonly Worker worker;
        private readonly long fileHeaderSize;

        public CompressFileWriter(string fileName,long fileHeaderSize, IThreadPool threadPool)
        {
            this.fileName = fileName;
            this.fileHeaderSize = fileHeaderSize;
            worker = new Worker(threadPool);
        }

        public void StartConsuming(IBlockingCollection<Stream> consumingBag) => worker.Run(() => Write(consumingBag));

        public void Wait() => worker.Wait();

        private void Write(IBlockingCollection<Stream> producingBag)
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Write);

            fileStream.Position += fileHeaderSize;
            while (producingBag.TryTake(out var stream))
                stream.CopyTo(fileStream);
        }

        public void Dispose()
        {
        }
    }
}