using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace GzipTest
{
    public class Program
    {
        // private const string FileName = @"C:\Users\kartsev\Documents\enwik8.txt";
        private const string FileName = @"C:\Users\kartsev\Documents\enwik9.txt";

        public static int Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string targetFileName = FileName.Replace(".txt", ".gz");

            using var fileWriter = new FileWriter(targetFileName);
            var reader = new FileReader(FileName, 8);
            var compressor = new Compressor(reader, fileWriter, 8);

            compressor.Start();
            compressor.Wait();

            stopwatch.Stop();
            Console.WriteLine($"elapsed: {stopwatch.Elapsed.Milliseconds} milliseconds");
            return 0;
        }
    }
}