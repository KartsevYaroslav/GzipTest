using System;
using System.Threading;

namespace GzipTest.Infrastructure
{
    public class Bounder : IDisposable
    {
        private readonly int capacity;
        private volatile int free;
        private readonly SemaphoreSlim semaphoreSlim;
        private readonly object lockObj;
        public bool IsRealised { get; private set; }

        public Bounder(int capacity)
        {
            this.capacity = capacity;
            free = 0;
            semaphoreSlim = new SemaphoreSlim(0, capacity);
            lockObj = new object();
        }

        public void WaitOne()
        {
            semaphoreSlim.Wait();
            Interlocked.Decrement(ref free);
        }


        public void ReleaseOne()
        {
            lock (lockObj)
            {
                CheckReleased();
                Interlocked.Increment(ref free);
            }

            semaphoreSlim.Release();
        }

        public void ReleaseAll()
        {
            int locked;
            lock (lockObj)
            {
                CheckReleased();
                IsRealised = true;
                locked = capacity - free;
            }

            if (locked > 0)
                semaphoreSlim.Release(locked);
        }

        private void CheckReleased()
        {
            if (IsRealised)
                throw new InvalidOperationException("Bounder already released");
        }

        public void Dispose() => semaphoreSlim.Dispose();
    }
}