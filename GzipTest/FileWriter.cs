using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GzipTest
{
    public class FileWriter : IWriter
    {
        private readonly string fileName;
        private FileStream? fileStream;
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
            fileStream ??= File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            var spinWait = new SpinWait();
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
            fileStream?.Flush();
            fileStream?.Dispose();
        }
    }
}