using System.IO;
using GzipTest.Decompress;
using GzipTest.Gzip;
using GzipTest.Infrastructure;

namespace GzipTest.Compress
{
    public class CompressFileWriter : IConsumer<Stream>
    {
        private readonly string fileName;
        private readonly Worker worker;

        public CompressFileWriter(string fileName, IThreadPool threadPool)
        {
            this.fileName = fileName;
            worker = new Worker(threadPool);
        }

        public void StartConsuming(IBlockingCollection<Stream> consumingBag) => worker.Run(() => Write(consumingBag));

        public void Wait() => worker.Wait();

        private void Write(IBlockingCollection<Stream> producingBag)
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Write);

            fileStream.Position += 8;
            while (producingBag.TryTake(out var stream))
                stream.CopyTo(fileStream);
        }

        public void Dispose()
        {
        }
    }
}