using System.IO;
using System.IO.Compression;
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
            using (var compressor = Gzip.Processor(CompressionMode.Compress, FileToZip, ZipFile))
            {
                compressor.Process();
            }

            using (var decompressor = Gzip.Processor(CompressionMode.Decompress, ZipFile, UnzipFile))
            {
                decompressor.Process();
            }

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