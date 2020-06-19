using System;

namespace GzipTest
{
    public interface IProducer<T> : IDisposable 
        where T : IDisposable
    {
        BlockingQueue<T> StartProducing();
        void Wait();
    }
}