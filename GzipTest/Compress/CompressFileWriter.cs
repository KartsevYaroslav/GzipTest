using System;
using System.IO;
using System.Threading;

namespace GzipTest.Compress
{
    public class CompressFileWriter : IConsumer<Stream>
    {
        private readonly string fileName;
        private BlockingQueue<Stream>? producingBag;
        private readonly Thread thread;

        public CompressFileWriter(string fileName)
        {
            this.fileName = fileName;
            thread = new Thread(Write);
        }

        public void StartConsuming(BlockingQueue<Stream> consumingQueue)
        {
            producingBag = consumingQueue;
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            producingBag = producingBag ?? throw new ArgumentNullException(nameof(producingBag));
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Write);

            fileStream.Position += 8;
            while (producingBag.TryTake(out var stream))
            {
                stream.CopyTo(fileStream);
            }
        }

        public void Dispose()
        {
        }
    }
}