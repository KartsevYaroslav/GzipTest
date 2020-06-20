using System;
using GzipTest.Infrastructure;

namespace GzipTest.Processor
{
    public interface IProducer<T> : IDisposable 
    {
        IBlockingCollection<T> StartProducing();
        void Wait();
    }
}