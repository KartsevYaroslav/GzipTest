using System;
using System.Threading;

namespace GzipTest
{
    public class Bounder: IDisposable
    {
        private readonly int capacity;
        private int free;
        private readonly SemaphoreSlim semaphoreSlim;
        private readonly AtomicBool isRealised;

        public Bounder(int capacity)
        {
            isRealised = new AtomicBool(false);
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

            semaphoreSlim.Release();
            Interlocked.Increment(ref free);
        }

        public void ReleaseAll()
        {
            CheckReleased();
            var spinner = new SpinWait();
            while (!isRealised.TrySet(true)) 
                spinner.SpinOnce();

            var locked = capacity - free;
            if (locked > 0)
                semaphoreSlim.Release(locked);
        }

        private void CheckReleased()
        {
            if (isRealised)
                throw new InvalidOperationException("Bounder already released");
        }

        public void Dispose()
        {
            semaphoreSlim.Dispose();
        }
    }
}