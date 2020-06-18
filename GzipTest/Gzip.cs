using System;
using System.IO;
using System.IO.Compression;
using GzipTest.Compress;
using GzipTest.Decompress;

namespace GzipTest
{
    public static class Gzip
    {
        public static IProcessor Processor(CompressionMode mode, string inputFileName, string outputFileName)
        {
            return mode switch
            {
                CompressionMode.Compress => CreateCompressor(inputFileName, outputFileName),
                CompressionMode.Decompress => CreateDecompressor(inputFileName, outputFileName),
                _ => throw new ArgumentException("Not supported mode")
            };
        }

        private static IProcessor CreateCompressor(string inputFileName, string outputFileName)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            fileStream.Write(sizeBytes);
            fileStream.Dispose();

            var fileWriter = new CompressFileWriter(outputFileName);
            var reader = new CompressFileReader(inputFileName);
            return new Compressor(reader, fileWriter, 8);
        }

        private static IProcessor CreateDecompressor(string inputFileName, string outputFileName)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);
            File.Create(outputFileName).Dispose();

            var reader = new DecompressFileReader(inputFileName);
            var decompressWriter = new DecompressFileWriter(outputFileName, fileSize, 16);
            return new Decompressor(reader, decompressWriter, 8);
        }
    }
}