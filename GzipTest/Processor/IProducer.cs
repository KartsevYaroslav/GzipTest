using System;
using GzipTest.Infrastructure;

namespace GzipTest.Gzip
{
    public interface IProducer<T> : IDisposable 
    {
        IBlockingCollection<T> StartProducing();
        void Wait();
    }
}