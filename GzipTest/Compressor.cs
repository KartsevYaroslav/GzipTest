using System;

namespace GzipTest
{
    public class Compressor : ICompressor,IDisposable
    {
        private readonly IReader reader;
        private readonly IWriter writer;
        private readonly uint concurrency;
        private CompressorWorkers? compressorWorkers;

        public Compressor(IReader reader, IWriter writer, uint concurrency)
        {
            this.reader = reader;
            this.writer = writer;
            this.concurrency = concurrency;
        }
        public void Start()
        {
            var queue = reader.StartReading();
            compressorWorkers ??= new CompressorWorkers(concurrency, queue, writer);
            compressorWorkers.Start();
        }

        public void Wait()
        {
            reader.Wait();
            compressorWorkers?.WaitAll();
        }

        public void Dispose()
        {
            compressorWorkers?.Dispose();
        }
    }
}