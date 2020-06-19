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
                throw new ArgumentException($"incorrect source file name {args[1]}");

            if (File.Exists(args[2]))
                File.Delete(args[2]);

            var worker = Gzip.Processor(mode, args[1], args[2]);
            worker.Process();
            stopwatch.Stop();
            Console.WriteLine($"elapsed {stopwatch.ElapsedMilliseconds}");
            return 0;
        }
    }
}