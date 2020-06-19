using System;
using System.IO;
using System.IO.Compression;

namespace GzipTest
{
    public class Chunk : IDisposable
    {
        public Chunk(long initialOffset, Stream content)
        {
            InitialOffset = initialOffset;
            Content = content;
        }

        public long InitialOffset { get; }
        public Stream Content { get; }

        public static Chunk FromCompressedStream(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.Read(buffer);
            var initialOffset = BitConverter.ToInt64(buffer);

            var memoryStream = new MemoryStream(1024 * 80);
            using var gZipStream = new GZipStream(stream, CompressionMode.Decompress);
            gZipStream.CopyTo(memoryStream, 1024 * 40);
            stream.Dispose();
            gZipStream.Close();
            memoryStream.Position = 0;

            return new Chunk(initialOffset, memoryStream);
        }

        public Stream ToCompressedStream()
        {
            var memoryStream = new MemoryStream(1024 * 80);

            memoryStream.Position += 4;
            var offsetBytes = BitConverter.GetBytes(InitialOffset);
            memoryStream.Write(offsetBytes);

            using var gZipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true);
            Content.CopyTo(gZipStream);
            gZipStream.Close();
            Content.Dispose();

            memoryStream.Position = 0;
            var chunkSize = BitConverter.GetBytes((int) memoryStream.Length - 12);
            memoryStream.Write(chunkSize);
            memoryStream.Position = 0;

            return memoryStream;
        }

        public void Dispose()
        {
            Content.Dispose();
        }
    }
}