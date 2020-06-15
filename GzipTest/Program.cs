using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace GzipTest
{
    public class Program
    {
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

            var worker = Gzip.Worker(mode, args[1], args[2]);
            worker.Start();
            worker.Wait();
            stopwatch.Stop();
            Console.WriteLine($"elapsed {stopwatch.ElapsedMilliseconds}");
            return 0;
        }
    }
}