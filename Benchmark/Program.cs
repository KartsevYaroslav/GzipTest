using System;
using System.IO;
using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GzipTest;
using GzipTest.Compress;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class CompressorReadAndWriteBenchmark
    {
        [Params(8)] public uint Concurrency;

        private const string Prefix = @"C:\Users\kartsev\Documents\";
        private const string FileToZip = Prefix + "enwik8.txt";
        private const string ZipFile = Prefix + "enwik8.gz";
        private const string FileToUnzip = Prefix + "enwik8_tmp.gz";
        private const string UnzipFile = Prefix + "enwik8_tmp.txt";

        [Benchmark]
        public void CompressReadAndWrite()
        {
            using var compressor = Gzip.Worker(CompressionMode.Compress, FileToZip, ZipFile);
        
            compressor.Start();
            compressor.Wait();
        }

        [Benchmark]
        public void DecompressReadAndWrite()
        {
            using var decompressor = Gzip.Worker(CompressionMode.Decompress, FileToUnzip, UnzipFile);

            decompressor.Start();
            decompressor.Wait();
        }

        [IterationSetup]
        public void SetUp()
        {
            File.Create(UnzipFile).Dispose();
            File.Create(ZipFile).Dispose();
        }

        [IterationCleanup]
        public void TearDown()
        {
            if (File.Exists(UnzipFile))
                File.Delete(UnzipFile);

            if (File.Exists(ZipFile))
                File.Delete(ZipFile);
        }

        public static class Program
        {
            public static void Main()
            {
                BenchmarkRunner.Run<CompressorReadAndWriteBenchmark>();
            }
        }
    }
}