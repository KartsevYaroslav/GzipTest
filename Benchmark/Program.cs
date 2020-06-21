using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GzipTest.Model;
using GzipTest.Processor;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class CompressorReadAndWriteBenchmark
    {
        private const string Prefix = @"C:\Users\kartsev\Documents\";
        private const string FileToZip = Prefix + "enwik8.txt";
        private const string ZipFile = Prefix + "enwik8.gz";
        private const string FileToUnzip = Prefix + "enwik8_tmp.gz";
        private const string UnzipFile = Prefix + "enwik8_tmp.txt";
        [Params("80", "1024")] public string BatchSize;

        [Benchmark]
        public void CompressReadAndWrite()
        {
            var args = UserArgs.ParseAndValidate(new[] {"compress", FileToZip, ZipFile, BatchSize});
            using var compressor = Gzip.Processor(args!);

            compressor.Process();
        }

        [Benchmark]
        public void DecompressReadAndWrite()
        {
            var args = UserArgs.ParseAndValidate(new[] {"decompress", FileToUnzip + BatchSize, UnzipFile, BatchSize});

            using var decompressor = Gzip.Processor(args!);

            decompressor.Process();
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