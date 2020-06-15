using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using GzipTest.Compress;

namespace GzipTest
{
    public class DecompressReader : IReader<Stream>
    {
        private readonly string fileName;
        private readonly BoundedList<Stream> queue;
        private readonly Thread thread;

        public DecompressReader(string fileName)
        {
            this.fileName = fileName;
            queue = new BoundedList<Stream>(8);

            thread = new Thread(ReadFile);
        }

        public BoundedList<Stream> StartReading()
        {
            thread.Start();
            return queue;
        }


        public void Wait()
        {
            thread.Join();
        }

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
                queue.Add(viewStream);
                offset += viewStream.Length + 4;
            }

            queue.CompleteAdding();
        }

        public void Dispose()
        {
        }
    }
}