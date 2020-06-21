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
        private const int FileHeaderSize = 11;
        public static readonly byte[] HeaderMagicNumber = {0x1f, 0x1f, 0x8b};

        public static IProcessor Processor(UserArgs userArgs, int? concurrency = null)
        {
            concurrency ??= Environment.ProcessorCount;
            return userArgs.CompressionMode switch
            {
                CompressionMode.Compress => CreateCompressor(userArgs, concurrency.Value),
                CompressionMode.Decompress => CreateDecompressor(userArgs, concurrency.Value),
                _ => throw new ArgumentException($"Not supported mode '{userArgs.CompressionMode}'")
            };
        }

        private static IProcessor CreateCompressor(UserArgs userArgs, int concurrency)
        {
            WriteHeader(userArgs.InputFileName, userArgs.OutputFileName);

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

        private static void WriteHeader(string inputFileName, string outputFileName)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            fileStream.Write(HeaderMagicNumber);
            fileStream.Write(sourceFileInfo.Length);
            fileStream.Dispose();
        }

        private static IProcessor CreateDecompressor(UserArgs userArgs, int concurrency)
        {
            var fileSize = ReadFileSize(userArgs.InputFileName);
            File.Create(userArgs.OutputFileName).Dispose();

            var threadPool = new BackgroundThreadPool(concurrency + 2);
            var reader = new DecompressFileReader(userArgs.InputFileName, FileHeaderSize, threadPool, concurrency);
            var decompressWriter = new DecompressFileWriter(
                threadPool,
                userArgs.OutputFileName,
                fileSize,
                concurrency * 2
            );

            return new Processor<Stream, Chunk>(
                reader,
                decompressWriter,
                threadPool,
                Chunk.FromCompressedStream,
                concurrency);
        }

        private static long ReadFileSize(string inputFileName)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            fileStream.Position += HeaderMagicNumber.Length;
            var fileSize = fileStream.ReadInt64();
            fileStream.Dispose();
            return fileSize;
        }
    }
}