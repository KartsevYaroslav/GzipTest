using System;
using System.IO;
using System.IO.Compression;

namespace GzipTest.Infrastructure
{
    public static class StreamExtensions
    {
        public static void CopyToAsync(
            this Stream source,
            Stream destination,
            BlockingBag<byte[]> buffersPool,
            Action<Stream, Stream> onComplete)
        {
            if (!buffersPool.TryTake(out var buffer))
                throw new InvalidOperationException();

            var readBytes = source.Read(buffer);

            if (readBytes > 0)
            {
                destination.BeginWrite(buffer, 0, readBytes, WriteCallback, null);
                return;
            }

            onComplete(source, destination);

            void WriteCallback(IAsyncResult writeResult)
            {
                destination.EndWrite(writeResult);
                buffersPool.Add(buffer);
                onComplete(source, destination);
            }
        }

        public static void CompressGzipTo(this Stream source, Stream target)
        {
            using var gZipStream = new GZipStream(target, CompressionLevel.Optimal, true);
            source.CopyTo(gZipStream);
            gZipStream.Close();
        }

        public static long ReadInt64(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.Read(buffer);
            return BitConverter.ToInt64(buffer);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.Read(buffer);
            return BitConverter.ToUInt32(buffer);
        }

        public static void Write(this Stream stream, long initialOffset)
        {
            var offsetBytes = BitConverter.GetBytes(initialOffset);
            stream.Write(offsetBytes);
        }

        public static void Write(this Stream stream, uint value)
        {
            var chunkSize = BitConverter.GetBytes(value);
            stream.Write(chunkSize);
        }

        public static void DecompressGzipTo(this Stream source, Stream target)
        {
            using var gZipStream = new GZipStream(source, CompressionMode.Decompress);
            gZipStream.CopyTo(target);
            gZipStream.Close();
        }
    }
}