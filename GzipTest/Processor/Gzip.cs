using System;
using System.IO;
using System.IO.Compression;
using GzipTest.Compress;
using GzipTest.Decompress;
using GzipTest.Infrastructure;
using GzipTest.Model;

namespace GzipTest.Gzip
{
    public static class Gzip
    {
        public static IProcessor Processor(
            CompressionMode mode,
            string inputFileName,
            string outputFileName,
            uint? concurrency = null
        )
        {
            concurrency ??= (uint) Environment.ProcessorCount;
            return mode switch
            {
                CompressionMode.Compress => CreateCompressor(inputFileName, outputFileName, concurrency.Value),
                CompressionMode.Decompress => CreateDecompressor(inputFileName, outputFileName, concurrency.Value),
                _ => throw new ArgumentException("Not supported mode")
            };
        }

        private static IProcessor CreateCompressor(string inputFileName, string outputFileName, uint concurrency)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            fileStream.Write(sizeBytes);
            fileStream.Dispose();

            var threadPool = new BackgroundThreadPool(concurrency + 2);
            var fileWriter = new CompressFileWriter(outputFileName, threadPool);
            var reader = new CompressFileReader(inputFileName, threadPool, concurrency);
            return new Processor<Chunk, Stream>(
                reader,
                fileWriter,
                threadPool,
                x => x.ToCompressedStream(),
                concurrency);
        }

        private static IProcessor CreateDecompressor(string inputFileName, string outputFileName, uint concurrency)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);
            File.Create(outputFileName).Dispose();

            var threadPool = new BackgroundThreadPool(concurrency + 1);
            var reader = new DecompressFileReader(inputFileName, threadPool, concurrency);
            var decompressWriter = new DecompressFileWriter(threadPool, outputFileName, fileSize, concurrency * 2);
            return new Processor<Stream, Chunk>(
                reader,
                decompressWriter,
                threadPool,
                Chunk.FromCompressedStream,
                concurrency);
        }
    }
}