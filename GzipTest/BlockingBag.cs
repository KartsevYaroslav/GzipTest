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
        private readonly Semaphore semaphore;

        public BlockingBag(int capacity)
        {
            buffer = new LinkedList<T>();
            lockObj = new object();
            semaphore = new Semaphore(capacity, capacity);
        }

        public bool TryTake(out T value)
        {
            value = default!;

            lock (lockObj)
            {
                if (Count == 0)
                    return false;

                if (buffer.First == null)
                    throw new InvalidOperationException("");

                value = buffer.First.Value;
                buffer.RemoveFirst();
                semaphore.Release();
                return true;
            }
        }

        public void Add(T value)
        {
            semaphore.WaitOne();
            lock (lockObj)
            {
                buffer.AddLast(value);
            }
        }

        public void CompleteAdding()
        {
            isCompleted = 1;
        }

        public bool IsAddingCompleted => isCompleted == 1;
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
            semaphore.Dispose();
            foreach (var element in buffer)
            {
                element.Dispose();
            }
        }
    }
}