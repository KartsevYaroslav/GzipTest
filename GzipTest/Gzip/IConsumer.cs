using System;
using GzipTest.Infrastructure;

namespace GzipTest.Gzip
{
    public interface IConsumer<T> : IDisposable
    {
        void StartConsuming(IBlockingCollection<T> consumingBag);
        void Wait();
    }
}