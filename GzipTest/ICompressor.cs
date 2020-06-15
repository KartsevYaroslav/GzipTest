
using System;

namespace GzipTest
{
    public interface IWorker : IDisposable
    {
        void Start();
        void Wait();
    }
}