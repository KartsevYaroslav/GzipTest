using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace GzipTest.Decompress
{
    public class DecompressFileWriter : IConsumer<Chunk>
    {
        private readonly string fileName;
        private readonly long fileSize;
        private readonly int concurrency;
        private BlockingBag<Chunk>? queue;
        private readonly List<DecompressWriteWorker> workers;
        private MemoryMappedFile? memoryMappedFile;

        public DecompressFileWriter(string fileName, long fileSize, int concurrency)
        {
            this.fileName = fileName;
            this.fileSize = fileSize;
            this.concurrency = concurrency;
            workers = new List<DecompressWriteWorker>();
        }

        public void StartConsuming(BlockingBag<Chunk> bag)
        {
            memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, fileSize,
                MemoryMappedFileAccess.ReadWrite);
            queue = bag;
            for (var i = 0; i < concurrency; i++)
            {
                var worker = new DecompressWriteWorker(memoryMappedFile, queue);
                worker.Start();
                workers.Add(worker);
            }
        }

        public void Wait()
        {
            foreach (var worker in workers)
            {
                worker.Wait();
            }
        }

        public void Dispose()
        {
            memoryMappedFile?.Dispose();
            queue?.Dispose();
        }
    }
}