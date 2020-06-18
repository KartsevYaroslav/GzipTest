using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest.Decompress
{
    public class DecompressFileReader : IProducer<Stream>
    {
        private readonly string fileName;
        private readonly BlockingBag<Stream> queue;
        private readonly Thread thread;

        public DecompressFileReader(string fileName)
        {
            this.fileName = fileName;
            queue = new BlockingBag<Stream>(8);

            thread = new Thread(ReadFile);
        }

        public BlockingBag<Stream> StartProducing()
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