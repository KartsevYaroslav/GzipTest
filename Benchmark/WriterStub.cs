using System;
using System.IO;
using GzipTest;

namespace Benchmark
{
    public class WriterStub : IWriter
    {
        public void Write(ReadOnlySpan<byte> bytes)
        {
        }

        public void Write(Stream stream)
        {
            
        }
    }
}