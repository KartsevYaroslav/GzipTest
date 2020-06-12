using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GzipTest
{
    public class FileWriter : IWriter<Stream>
    {
        private readonly string fileName;
        private BlockingCollection<Stream>? queue;
        private Thread thread;

        public FileWriter(string fileName)
        {
            this.fileName = fileName;
            thread = new Thread(Write);
        }

        public void Start(BlockingCollection<Stream> streams)
        {
            queue = streams;
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            using var fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write);

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