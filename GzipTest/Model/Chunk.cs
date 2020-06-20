using System;
using System.IO;
using GzipTest.Infrastructure;

namespace GzipTest.Model
{
    public class Chunk : IDisposable
    {
        private const int DefaultBufferSize = 1024 * 1024;

        public Chunk(long initialOffset, Stream content)
        {
            InitialOffset = initialOffset;
            Content = content;
        }

        public long InitialOffset { get; }
        public Stream Content { get; }

        public static Chunk FromCompressedStream(Stream stream)
        {
            var initialOffset = stream.ReadInt64();

            var memoryStream = new MemoryStream(DefaultBufferSize);
            stream.DecompressGzipTo(memoryStream);
            stream.Dispose();
            memoryStream.Position = 0;

            return new Chunk(initialOffset, memoryStream);
        }

        public Stream ToCompressedStreamWithSize()
        {
            const int headerLength = 12;

            var memoryStream = new MemoryStream(checked((int) Content.Length));
            memoryStream.Position += headerLength;

            Content.CompressGzipTo(memoryStream);
            Content.Dispose();
            memoryStream.Position = 0;

            memoryStream.Write(checked((int) memoryStream.Length) - headerLength);
            memoryStream.Write(InitialOffset);
            memoryStream.Position = 0;

            return memoryStream;
        }

        public void Dispose() => Content.Dispose();
    }
}