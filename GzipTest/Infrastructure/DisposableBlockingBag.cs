using System;

namespace GzipTest.Infrastructure
{
    public class DisposableBlockingBag<T> : IBlockingCollection<T>
        where T : IDisposable
    {
        private readonly BlockingBag<T> innerBag;

        public DisposableBlockingBag(uint concurrency) => innerBag = new BlockingBag<T>(concurrency);

        public bool TryTake(out T value) => innerBag.TryTake(out value);

        public void Add(T value) => innerBag.Add(value);
        public void CompleteAdding() => innerBag.CompleteAdding();

        public void Dispose()
        {
            foreach (var element in innerBag)
            {
                element.Dispose();
            }
            innerBag.Dispose();
        }
    }
}