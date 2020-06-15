using System;
using System.IO;
using System.IO.Compression;
using GzipTest.Compress;

namespace GzipTest
{
    public static class Gzip
    {
        public static IWorker Worker(CompressionMode mode, string inputFileName, string outputFileName)
        {
            return mode switch
            {
                CompressionMode.Compress => CreateCompressor(inputFileName, outputFileName),
                CompressionMode.Decompress => CreateDecompressor(inputFileName, outputFileName),
                _ => throw new ArgumentException("Not supported mode")
            };
        }

        private static IWorker CreateCompressor(string inputFileName, string outputFileName)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            fileStream.Write(sizeBytes);
            fileStream.Dispose();

            using var fileWriter = new FileWriter(outputFileName);
            var reader = new FileReader(inputFileName);
            return new Compressor(reader, fileWriter, 8);
        }

        private static IWorker CreateDecompressor(string inputFileName, string outputFileName)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);
            File.Create(outputFileName).Dispose();

            var reader = new DecompressReader(inputFileName);
            var decompressWriter = new DecompressWriter(outputFileName, fileSize, 16);
            return new Decompressor(reader, decompressWriter, 8);
        }
    }
}