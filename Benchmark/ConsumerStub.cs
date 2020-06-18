using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest;
using GzipTest.Compress;

namespace Benchmark
{
    public class ConsumerStub : IConsumer<Stream>
    {
        public void Start(BlockingCollection<Stream> streams)
        {
        }

        public void StartConsuming(BlockingBag<Stream> bag)
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