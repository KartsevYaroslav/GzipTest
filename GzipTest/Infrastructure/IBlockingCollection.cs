using System;

namespace GzipTest.Infrastructure
{
    public interface IBlockingCollection<T> : IDisposable
    {
        bool TryTake(out T value);
        void Add(T value);
        void CompleteAdding();
    }
}