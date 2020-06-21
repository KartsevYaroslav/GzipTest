using System;

namespace GzipTest.Processor
{
    public interface IProcessor : IDisposable
    {
        void Process();
    }
}