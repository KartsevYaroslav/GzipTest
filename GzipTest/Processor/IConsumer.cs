using System;
using GzipTest.Infrastructure;

namespace GzipTest.Processor
{
    public interface IConsumer<T> : IDisposable
    {
        void StartConsuming(IBlockingCollection<T> consumingBag);
        void Wait();
    }
}