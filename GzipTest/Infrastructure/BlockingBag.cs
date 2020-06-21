using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace GzipTest.Infrastructure
{
    public class BlockingBag<T> : IBlockingCollection<T>, IEnumerable<T>
    {
        private readonly SemaphoreSlim addBounder;
        private readonly LinkedList<T> buffer;
        private readonly object lockObj;
        private readonly Bounder takeBounder;

        public BlockingBag(int capacity)
        {
            buffer = new LinkedList<T>();
            lockObj = new object();
            addBounder = new SemaphoreSlim(capacity, capacity);
            takeBounder = new Bounder(capacity);
        }

        private bool IsAddingComplete => takeBounder.IsRealised;

        public bool TryTake(out T value)
        {
            value = default!;

            if (!IsAddingComplete)
                takeBounder.WaitOne();

            lock (lockObj)
            {
                if (IsAddingComplete && buffer.Count == 0)
                    return false;

                if (buffer.First == null)
                    throw new InvalidOperationException("Cannot take value, node is null");

                value = buffer.First.Value;
                buffer.RemoveFirst();
                addBounder.Release();
                return true;
            }
        }

        public void Add(T value)
        {
            addBounder.Wait();
            lock (lockObj)
            {
                buffer.AddLast(value);
            }

            takeBounder.ReleaseOne();
        }

        public void CompleteAdding()
        {
            takeBounder.ReleaseAll();
        }

        public void Dispose()
        {
            if (!IsAddingComplete)
                CompleteAdding();

            addBounder.Dispose();
            takeBounder.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (buffer)
            {
                return buffer.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}