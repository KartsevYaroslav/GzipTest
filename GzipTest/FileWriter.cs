using System;
using System.IO;

namespace GzipTest
{
    public class FileWriter : IWriter, IDisposable
    {
        private readonly string fileName;
        private readonly object lockObj;
        private FileStream? fileStream;

        public FileWriter(string fileName)
        {
            this.fileName = fileName;
            lockObj = new object();
        }

        public void Write(Stream stream)
        {
            lock (lockObj)
            {
                fileStream ??= File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write);
                stream.CopyTo(fileStream);
            }
        }

        public void Dispose()
        {
            fileStream?.Flush();
            fileStream?.Dispose();
        }
    }
}