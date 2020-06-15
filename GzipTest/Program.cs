using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace GzipTest
{
    public class Program
    {
        // private const string FileName = @"C:\Users\kartsev\Documents\enwik8.txt";
        // private const string FileName = @"C:\Users\kartsev\Documents\enwik9.txt";
        // private static readonly string TargetFileName = FileName.Replace(".txt", ".gz");


        public static int Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!Enum.TryParse(args[0], true, out CompressionMode mode))
                throw new ArgumentException("Incorrect arguments");

            if (!File.Exists(args[1]))
                throw new ArgumentException("incorrect source file name");

            if (File.Exists(args[2]))
                File.Delete(args[2]);


            var res = mode switch
            {
                CompressionMode.Compress => Compress(args[1], args[2]),
                CompressionMode.Decompress => Decompress(args[1], args[2]),
                _ => throw new ArgumentException("Not supported mode")
            };

            stopwatch.Stop();
            Console.WriteLine($"elapsed {stopwatch.ElapsedMilliseconds}");
            return res;
        }

        private static int Decompress(string inputFileName, string outputFileName)
        {
            var fileStream = File.Open(inputFileName, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);

            File.Create(outputFileName).Dispose();

            var reader = new DecompressReader(inputFileName);
            var decompressWriter = new DecompressWriter(outputFileName, fileSize, 8);
            var decompressor = new Decompressor(reader, decompressWriter, 8);

            decompressor.Start();
            decompressor.Wait();

            Console.WriteLine("finished");
            return 0;
        }

        private static int Compress(string inputFileName, string outputFileName)
        {
            var sourceFileInfo = new FileInfo(inputFileName);
            var fileStream = File.Create(outputFileName);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            fileStream.Write(sizeBytes);
            fileStream.Dispose();

            using var fileWriter = new FileWriter(outputFileName);
            var reader = new FileReader(inputFileName);
            var compressor = new Compressor(reader, fileWriter, 8);

            compressor.Start();
            compressor.Wait();

            Console.WriteLine("finished");
            return 0;
        }
    }
}