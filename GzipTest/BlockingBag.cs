using System;
using System.Collections.Generic;
using System.Threading;

namespace GzipTest
{
    public class BlockingBag<T> : IDisposable
        where T : IDisposable
    {
        private readonly LinkedList<T> buffer;
        private readonly object lockObj;
        private int isCompleted;
        private readonly Semaphore addLimiter;
        private readonly Bounder takeLimiter;

        public BlockingBag(int capacity)
        {
            buffer = new LinkedList<T>();
            lockObj = new object();
            addLimiter = new Semaphore(capacity, capacity);
            takeLimiter = new Bounder(capacity);
        }

        public bool TryTake(out T value)
        {
            value = default!;

            if (!IsAddingCompleted)
                takeLimiter.WaitOne();

            lock (lockObj)
            {
                if (IsAddingCompleted && buffer.Count == 0)
                    return false;

                if (buffer.First == null)
                    throw new InvalidOperationException("");

                value = buffer.First.Value;
                buffer.RemoveFirst();
                addLimiter.Release();
                return true;
            }
        }

        public void Add(T value)
        {
            addLimiter.WaitOne();
            lock (lockObj)
            {
                buffer.AddLast(value);
            }
            takeLimiter.ReleaseOne();
        }

        public void CompleteAdding()
        {
            takeLimiter.ReleaseAll();
            Interlocked.CompareExchange(ref isCompleted, 1, 0);
        }

        public bool IsAddingCompleted => Interlocked.CompareExchange(ref isCompleted, 0, 0) != 0;
        public int Count => GetCount();

        private int GetCount()
        {
            lock (lockObj)
            {
                return buffer.Count;
            }
        }

        public void Dispose()
        {
            addLimiter.Dispose();
            takeLimiter.Dispose();
            foreach (var element in buffer)
            {
                element.Dispose();
            }
        }
    }
}