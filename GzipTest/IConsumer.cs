using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest.Compress;

namespace GzipTest
{
    public interface IConsumer<T> : IDisposable
        where T : IDisposable
    {
        void StartConsuming(BlockingQueue<T> consumingQueue);
        void Wait();
    }
}