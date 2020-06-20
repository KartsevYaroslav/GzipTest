using System;
using System.IO;
using System.IO.Compression;
using GzipTest.Compress;
using GzipTest.Decompress;
using GzipTest.Infrastructure;
using GzipTest.Model;

namespace GzipTest.Processor
{
    public static class Gzip
    {
        public static readonly byte[] HeaderMagicNumber = {0x1f, 0x1f, 0x8b};

        private const int FileHeaderSize = 15;

        public static IProcessor Processor(UserArgs userArgs, uint? concurrency = null)
        {
            concurrency ??= (uint) Environment.ProcessorCount;
            return userArgs.CompressionMode switch
            {
                CompressionMode.Compress => CreateCompressor(userArgs, concurrency.Value),
                CompressionMode.Decompress => CreateDecompressor(userArgs, concurrency.Value),
                _ => throw new ArgumentException($"Not supported mode '{userArgs.CompressionMode}'")
            };
        }

        private static IProcessor CreateCompressor(UserArgs userArgs, uint concurrency)
        {
            WriteHeader(userArgs.InputFileName, userArgs.OutputFileName, userArgs.BatchSize / 1024);

            var threadPool = new BackgroundThreadPool(concurrency + 2);
            var fileWriter = new CompressFileWriter(userArgs.OutputFileName, FileHeaderSize, threadPool);
            var reader = new CompressFileReader(userArgs.InputFileName, userArgs.BatchSize, threadPool, concurrency);
            return new Processor<Chunk, Stream>(
                reader,
                fileWriter,
                threadPool,
                x => x.ToCompressedStreamWithSize(),
                concurrency);
        }

        private static void WriteHeader(string inputFileName, string outputFileName, uint chunkSize)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            fileStream.Write(HeaderMagicNumber);
            fileStream.Write(sourceFileInfo.Length);
            fileStream.Write(chunkSize);
            fileStream.Dispose();
        }

        private static IProcessor CreateDecompressor(UserArgs userArgs, uint concurrency)
        {
            var (fileSize, chunkSize) = ReadHeader(userArgs.InputFileName);
            File.Create(userArgs.OutputFileName).Dispose();

            var threadPool = new BackgroundThreadPool(concurrency + 2);
            var reader = new DecompressFileReader(userArgs.InputFileName, FileHeaderSize, threadPool, concurrency);
            var decompressWriter = new DecompressFileWriter(
                threadPool,
                userArgs.OutputFileName,
                fileSize,
                chunkSize * 1024,
                concurrency * 2
            );

            return new Processor<Stream, Chunk>(
                reader,
                decompressWriter,
                threadPool,
                Chunk.FromCompressedStream,
                concurrency);
        }

        private static (long fileSize, uint chunkSize) ReadHeader(string inputFileName)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            fileStream.Position += HeaderMagicNumber.Length;
            var fileSize = fileStream.ReadInt64();
            var chunkSize = fileStream.ReadUInt32();
            fileStream.Dispose();
            return (fileSize, chunkSize);
        }
    }
}