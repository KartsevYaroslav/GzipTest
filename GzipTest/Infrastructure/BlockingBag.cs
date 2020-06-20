﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace GzipTest.Infrastructure
{
    public class BlockingBag<T> : IBlockingCollection<T>, IEnumerable<T>
    {
        private readonly LinkedList<T> buffer;
        private readonly object lockObj;
        private AtomicBool IsAddingComplete => takeLimiter.IsRealised;
        private readonly SemaphoreSlim addLimiter;
        private readonly Bounder takeLimiter;

        public BlockingBag(uint capacity)
        {
            buffer = new LinkedList<T>();
            lockObj = new object();
            addLimiter = new SemaphoreSlim((int) capacity, (int) capacity);
            takeLimiter = new Bounder((int) capacity);
        }

        public bool TryTake(out T value)
        {
            value = default!;

            if (!IsAddingComplete)
                takeLimiter.WaitOne();

            lock (lockObj)
            {
                if (IsAddingComplete && buffer.Count == 0)
                    return false;

                if (buffer.First == null)
                    throw new InvalidOperationException("Cannot deque value, node is null");

                value = buffer.First.Value;
                buffer.RemoveFirst();
                addLimiter.Release();
                return true;
            }
        }

        public void Add(T value)
        {
            addLimiter.Wait();
            lock (lockObj)
            {
                buffer.AddLast(value);
            }

            takeLimiter.ReleaseOne();
        }

        public void CompleteAdding()
        {
            takeLimiter.ReleaseAll();
        }

        public void Dispose()
        {
            if (!IsAddingComplete)
                CompleteAdding();

            addLimiter.Dispose();
            takeLimiter.Dispose();
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