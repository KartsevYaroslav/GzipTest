using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using GzipTest.Processor;

namespace GzipTest.Model
{
    public class UserArgs
    {
        public static UserArgs? ParseAndValidate(string[] args)
        {
            if (args.Length == 0 || args[0] == "help")
            {
                Console.WriteLine(HelpMessage);
                return null;
            }

            if (args.Length < 3)
            {
                Console.WriteLine("Incorrect count of arguments. Should be '<mode> <input file> <output file>");
                return null;
            }

            if (!Enum.TryParse(args[0], true, out CompressionMode mode))
            {
                Console.WriteLine($"Incorrect mode '{args[0]}', supported 'compress' and 'decompress'");
                return null;
            }

            if (!ValidateSourceFileName(args[1], mode))
                return null;

            if (File.Exists(args[2]))
            {
                Console.WriteLine($"File already exists '{args[2]}'");
                return null;
            }

            uint? batchSize = null;
            if (args.Length > 4 && !ValidateBatchSize(args[3..5], out batchSize))
                return null;

            return new UserArgs(mode, args[1], args[2], batchSize);
        }

        private const string HelpMessage =
            "Usage:\n  GzipTest.exe [command] [input file name] [output file name] [options]\n\n" +
            "Commands:\n  compress\n  decompress\n  help\n\n" +
            "Options:\n  -b [arg]\tblock size in kb. Default value 1024 (only for compress mode)";

        private static bool ValidateBatchSize(IReadOnlyList<string> batchSizeArgs, out uint? batchSize)
        {
            batchSize = null;
            if (batchSizeArgs[0] != "-b")
                return true;

            if (!uint.TryParse(batchSizeArgs[1], out var parsed))
            {
                Console.WriteLine($"Incorrect batch size value {batchSizeArgs[1]}. Should be positive integer");
                return false;
            }

            batchSize = parsed * 1024;
            return true;
        }

        private static bool ValidateSourceFileName(string fileName, CompressionMode mode)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Incorrect source file name '{fileName}'");
                return false;
            }

            try
            {
                var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                if (mode == CompressionMode.Decompress)
                {
                    Span<byte> span = stackalloc byte[Gzip.HeaderMagicNumber.Length];
                    fileStream.Read(span);
                    if (!span.SequenceEqual(Gzip.HeaderMagicNumber))
                    {
                        Console.WriteLine("Unknown file format");
                        fileStream.Close();
                        return false;
                    }
                }

                fileStream.Close();
            }
            catch (IOException)
            {
                Console.WriteLine($"File '{fileName}' locked by another process");
                return false;
            }

            return true;
        }

        private UserArgs(
            CompressionMode compressionMode,
            string inputFileName,
            string outputFileName,
            uint? batchSize = null)
        {
            CompressionMode = compressionMode;
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            BatchSize = batchSize ?? 1024 * 1024;
        }

        public string InputFileName { get; }
        public string OutputFileName { get; }
        public uint BatchSize { get; }
        public CompressionMode CompressionMode { get; }
    }
}