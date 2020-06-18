
using System;

namespace GzipTest
{
    public interface IProcessor : IDisposable
    {
        void Process();
    }
}