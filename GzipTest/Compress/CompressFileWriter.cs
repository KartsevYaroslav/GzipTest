using System.IO;
using System.Threading;

namespace GzipTest.Compress
{
    public class CompressFileWriter : IConsumer<Stream>
    {
        private readonly string fileName;
        private BlockingBag<Stream>? queue;
        private readonly Thread thread;

        public CompressFileWriter(string fileName)
        {
            this.fileName = fileName;
            thread = new Thread(Write);
        }

        public void StartConsuming(BlockingBag<Stream> bag)
        {
            queue = bag;
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Write);

            var spinWait = new SpinWait();
            fileStream.Position += 8;
            while (true)
            {
                if (queue.IsAddingCompleted && queue.Count == 0)
                    break;

                if (!queue.TryTake(out var stream))
                {
                    spinWait.SpinOnce();
                    continue;
                }

                spinWait = new SpinWait();

                stream.CopyTo(fileStream);
            }
        }

        public void Dispose()
        {
        }
    }
}