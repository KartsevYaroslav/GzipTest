
using System;

namespace GzipTest.Gzip
{
    public interface IProcessor : IDisposable
    {
        void Process();
    }
}