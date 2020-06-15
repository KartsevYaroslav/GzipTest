using System;
using System.Collections.Concurrent;

namespace GzipTest.Compress
{
    public interface IReader<T> : IDisposable
    {
        BoundedList<T> StartReading();
        void Wait();
    }
}