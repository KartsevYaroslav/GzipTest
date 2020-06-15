using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest.Compress;

namespace GzipTest
{
    public interface IWriter<T>: IDisposable
    {
        void Start(BoundedList<T> streams);
        void Wait();
    }
}