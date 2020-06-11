using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest;

namespace Benchmark
{
    public class WriterStub : IWriter
    {
        public void Start(BlockingCollection<Stream> streams)
        {
        }

        public void Wait()
        {
        }

        public void Dispose()
        {
        }
    }
}