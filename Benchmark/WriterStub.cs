using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest;
using GzipTest.Compress;

namespace Benchmark
{
    public class WriterStub : IWriter<Stream>
    {
        public void Start(BlockingCollection<Stream> streams)
        {
        }

        public void Start(BoundedList<Stream> streams)
        {
            throw new NotImplementedException();
        }

        public void Wait()
        {
        }

        public void Dispose()
        {
        }
    }
}