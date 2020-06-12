using System;
using System.IO;
using System.IO.Compression;

namespace GzipTest
{
    public class Program
    {
        // private const string FileName = @"C:\Users\kartsev\Documents\enwik8.txt";
        private const string FileName = @"C:\Users\kartsev\Documents\enwik9.txt";
        private static readonly string TargetFileName = FileName.Replace(".txt", ".gz");


        public static int Main(string[] args)
        {
            if (!Enum.TryParse(args[0], true, out CompressionMode mode))
                throw new ArgumentException("Incorrect arguments");


            return mode switch
            {
                CompressionMode.Compress => Compress(),
                CompressionMode.Decompress => Decompress(),
                _ => throw new ArgumentException("Not supported mode")
            };
        }

        private static int Decompress()
        {
            var fileStream = File.Open(TargetFileName, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);

            const string newFileName = FileName + "_tmp";
            if (File.Exists(newFileName))
                File.Delete(newFileName);

            File.Create(newFileName).Dispose();

            var reader = new DecompressReader(TargetFileName);
            var decompressWriter = new DecompressWriter(newFileName, fileSize);
            var decompressor = new Decompressor(reader, decompressWriter, 8);

            decompressor.Start();
            decompressor.Wait();

            Console.WriteLine("finished");
            return 0;
        }

        private static int Compress()
        {
            if (File.Exists(TargetFileName))
                File.Delete(TargetFileName);

            var sourceFileInfo = new FileInfo(FileName);
            var fileStream = File.Create(TargetFileName);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            fileStream.Write(sizeBytes);
            fileStream.Dispose();

            using var fileWriter = new FileWriter(TargetFileName);
            var reader = new FileReader(FileName);
            var compressor = new Compressor(reader, fileWriter, 8);

            compressor.Start();
            compressor.Wait();

            Console.WriteLine("finished");
            return 0;
        }
    }
}