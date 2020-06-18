using System;

namespace GzipTest
{
    public interface IProducer<T> : IDisposable 
        where T : IDisposable
    {
        BlockingBag<T> StartProducing();
        void Wait();
    }
}