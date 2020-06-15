using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest
{
    public class DecompressReader : IReader<Stream>
    {
        private readonly string fileName;
        private readonly BlockingCollection<Stream> queue;
        private readonly Thread thread;

        public DecompressReader(string fileName)
        {
            this.fileName = fileName;
            queue = new BlockingCollection<Stream>();

            thread = new Thread(ReadFile);
        }

        public BlockingCollection<Stream> StartReading()
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
            //
            // using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            // fileStream.Position += 8;
            //
            // var remainSize = fileStream.Length;
            // var memoryPool = MemoryPool<byte>.Shared;
            // using var memoryOwner = memoryPool.Rent(1024 * 80);
            // var buffer = memoryOwner.Memory.Span;
            // var sizeBuffer = buffer.Slice(0, 4);
            // while (remainSize > 0)
            // {
            //     var memoryStream = new MemoryStream();
            //     fileStream.Read(sizeBuffer);
            //
            //     var size = BitConverter.ToInt32(sizeBuffer);
            //     var contentBuffer = buffer.Slice(0, size + 8);
            //     var readBytes = fileStream.Read(contentBuffer);
            //     memoryStream.Write(contentBuffer.Slice(0, readBytes));
            //     queue.Add(memoryStream);
            //
            //     remainSize -= readBytes;
            // }

            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null);

            var offset = 8L;

            Span<byte> sizeBuffer = stackalloc byte[4];
            var spinWait = new SpinWait();
            while (offset < fileInfo.Length)
            {
                if (queue.Count > 10)
                {
                    spinWait.SpinOnce();
                    continue;
                }

                spinWait = new SpinWait();
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
    }
}