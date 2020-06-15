using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace GzipTest.Tests
{
    public class FunctionalTests
    {
        private const string Prefix = @"C:\Users\kartsev\Documents\";
        private const string FileToZip = Prefix + "enwik7.txt";
        private const string ZipFile = Prefix + "enwik7.gz";
        private const string UnzipFile = Prefix + "enwik7_tmp";

        [SetUp]
        public void SetUp()
        {
            File.Create(UnzipFile).Dispose();
            File.Create(ZipFile).Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(UnzipFile))
                File.Delete(UnzipFile);

            if (File.Exists(ZipFile))
                File.Delete(ZipFile);
        }

        [Test]
        public void Should_compress_and_decompress()
        {
            var sourceFileInfo = new FileInfo(FileToZip);
            var sourceStream = File.Create(ZipFile);
            var sizeBytes = BitConverter.GetBytes(sourceFileInfo.Length);
            sourceStream.Write(sizeBytes);
            sourceStream.Dispose();
            using (var fileWriter = new FileWriter(ZipFile))
            {
                var reader = new FileReader(FileToZip);
                using (var compressor = new Compressor(reader, fileWriter, 8))
                {
                    compressor.Start();
                    compressor.Wait();
                }
            }


            var fileStream = File.Open(ZipFile, FileMode.Open);
            Span<byte> buffer = stackalloc byte[8];
            fileStream.Read(buffer);
            fileStream.Dispose();
            var fileSize = BitConverter.ToInt64(buffer);
            var decompressReader = new DecompressReader(ZipFile);
            var decompressWriter = new DecompressWriter(UnzipFile, fileSize, 8);
            using (var decompressor = new Decompressor(decompressReader, decompressWriter, 8))
            {
                decompressor.Start();
                decompressor.Wait();
            }

            decompressWriter.Dispose();

            FileEquals().Should().BeTrue();
        }

        private static bool FileEquals()
        {
            var sourceInfo = new FileInfo(FileToZip);
            var targetInfo = new FileInfo(UnzipFile);

            if (sourceInfo.Length != targetInfo.Length)
                return false;

            using var source = sourceInfo.OpenRead();
            using var target = targetInfo.OpenRead();

            for (var i = 0; i < source.Length; i++)
            {
                var readByte = source.ReadByte();
                var b = target.ReadByte();
                if (readByte != b)
                    return false;
            }

            return true;
        }
    }
}