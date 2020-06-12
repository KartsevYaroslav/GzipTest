using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GzipTest;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class CompressorReadAndWriteBenchmark
    {
        [Params(8)] public uint Concurrency;

        [Benchmark]
        public void CompressReadAndWrite()
        {
            const string fileName = @"C:\Users\kartsev\Documents\enwik8.txt";
            // const string fileName = @"C:\Users\kartsev\Documents\enwik9.txt";
            var targetFileName = fileName.Replace(".txt", ".gz");

            using var fileWriter = new FileWriter(targetFileName);
            var reader = new FileReader(fileName);
            var compressor = new Compressor(reader, fileWriter, Concurrency);

            compressor.Start();
            compressor.Wait();
        }

        [MemoryDiagnoser]
        public class CompressorBenchmark
        {
            [Params(4096, 65536, 1_048_576)] public uint ChunkSize;

            [GlobalSetup]
            public void SetUp()
            {
            }


            // [Benchmark]
            // public void Compress()
            // {
            //     const uint totalSize = 10_000_000;
            //     var fileWriter = new WriterStub();
            //     var reader = new ReaderStub(ChunkSize, totalSize / ChunkSize);
            //     var compressor = new Compressor(reader, fileWriter, 8);
            //
            //     compressor.Start();
            //     compressor.Wait();
            // }
        }

        public static class Program
        {
            public static void Main()
            {
                // BenchmarkRunner.Run<CompressorBenchmark>();
                BenchmarkRunner.Run<CompressorReadAndWriteBenchmark>();
            }
        }
    }
}