﻿using System;
using System.Collections.Concurrent;
using System.IO;
using GzipTest;

namespace Benchmark
{
    public class ReaderStub : IReader
    {
        private readonly uint chunkSize;
        private readonly uint chunksCount;

        public ReaderStub(uint chunkSize, uint chunksCount)
        {
            this.chunkSize = chunkSize;
            this.chunksCount = chunksCount;
        }

        public BlockingCollection<Chunk> StartReading()
        {
            var random = new Random();
            var queue = new BlockingCollection<Chunk>();
            for (var i = 0; i < chunksCount; i++)
            {
                var bytes = new byte[chunkSize];
                random.NextBytes(bytes);
                queue.Add(new Chunk(0, new MemoryStream(bytes)));
            }

            queue.CompleteAdding();
            return queue;
        }

        public void Wait()
        {
        }
    }
}