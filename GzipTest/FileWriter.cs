using System;
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
            while (true)
            {
                if (queue.IsAddingCompleted && queue.Count == 0)
                    break;

                var stream = queue.Take();
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