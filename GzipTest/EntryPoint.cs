using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using GzipTest.Decompress;
using GzipTest.Infrastructure;
using GzipTest.Processor;

namespace GzipTest
{
    public class Args
    {
        public static Args? Parse(string[] args, ILog log)
        {
            if (args.Length < 3)
            {
                log.UserError("Incorrect count of arguments. Should be '<mode> <input file> <output file>");
                return null;
            }

            if (!Enum.TryParse(args[0], true, out CompressionMode mode))
            {
                log.UserError($"Incorrect mode '{mode}', supported 'compress' and 'decompress'");
                return null;
            }

            if (!File.Exists(args[1]))
            {
                log.UserError($"Incorrect source file name {args[1]}");
                return null;
            }

            if (File.Exists(args[2]))
            {
                File.Delete(args[2]);
                log.UserError($"File already exists {args[2]}");
                // return null;
            }

            return new Args(mode, args[1], args[2]);
        }

        private Args(CompressionMode compressionMode, string inputFileName, string outputFileName)
        {
            CompressionMode = compressionMode;
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
        }

        public string InputFileName { get; }
        public string OutputFileName { get; }
        public CompressionMode CompressionMode { get; }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            var isVerbose = args.Length == 4 && args[3] == "-v";
            var log = new ConsoleLog(isVerbose);
            var arguments = Args.Parse(args, log);
            if (arguments == null)
                return 1;

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();


                var processor = Gzip.Processor(arguments);
                processor.Process();
                stopwatch.Stop();

                Console.WriteLine($"elapsed {stopwatch.ElapsedMilliseconds}");
                return 0;
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }
        }
    }
}