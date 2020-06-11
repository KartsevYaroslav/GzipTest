using System;
using System.Collections.Concurrent;
using System.IO;

namespace GzipTest
{
    public interface IWriter: IDisposable
    {
        void Start(BlockingCollection<Stream> streams);
        void Wait();
    }
}