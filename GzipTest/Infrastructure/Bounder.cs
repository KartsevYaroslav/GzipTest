using System;
using System.Threading;

namespace GzipTest.Infrastructure
{
    public class Bounder : IDisposable
    {
        private readonly int capacity;
        private volatile int free;
        private readonly SemaphoreSlim semaphoreSlim;
        public AtomicBool IsRealised { get; }

        public Bounder(int capacity)
        {
            IsRealised = new AtomicBool(false);
            this.capacity = capacity;
            free = 0;
            semaphoreSlim = new SemaphoreSlim(0, capacity);
        }

        public void WaitOne()
        {
            semaphoreSlim.Wait();
            Interlocked.Decrement(ref free);
        }


        public void ReleaseOne()
        {
            CheckReleased();

            Interlocked.Increment(ref free);
            semaphoreSlim.Release();
        }

        public void ReleaseAll()
        {
            CheckReleased();
            IsRealised.Set(true);

            var locked = capacity - free;
            if (locked > 0)
                semaphoreSlim.Release(locked);
        }

        private void CheckReleased()
        {
            if (IsRealised)
                throw new InvalidOperationException("Bounder already released");
        }

        public void Dispose()
        {
            semaphoreSlim.Dispose();
        }
    }
}