using System;
using System.Collections.Concurrent;
using System.IO;

namespace GzipTest
{
    public interface IWriter<T>: IDisposable
    {
        void Start(BlockingCollection<T> streams);
        void Wait();
    }
}