using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest.Compress;

namespace GzipTest
{
    public interface IConsumer<T> : IDisposable
        where T : IDisposable
    {
        void StartConsuming(BlockingBag<T> bag);
        void Wait();
    }
}