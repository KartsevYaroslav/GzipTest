using System;
using System.IO;

namespace GzipTest
{
    public interface IWriter
    {
        void Write(Stream stream);
    }
}