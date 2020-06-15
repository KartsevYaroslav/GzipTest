using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace GzipTest
{
    public class DecompressWriter : IWriter<Chunk>
    {
        private readonly string fileName;
        private readonly long fileSize;
        private readonly int concurrency;
        private FileStream? fileStream;
        private BlockingCollection<Chunk>? queue;
        private readonly List<DecompressWriteWorker> workers;
        private MemoryMappedFile? memoryMappedFile;

        public DecompressWriter(string fileName, long fileSize, int concurrency)
        {
            this.fileName = fileName;
            this.fileSize = fileSize;
            this.concurrency = concurrency;
            workers = new List<DecompressWriteWorker>();
        }

        public void Start(BlockingCollection<Chunk> streams)
        {
            memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, fileSize,
                MemoryMappedFileAccess.ReadWrite);
            queue = streams;
            for (int i = 0; i < concurrency; i++)
            {
                var worker = new DecompressWriteWorker(memoryMappedFile, fileSize, queue);
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

        // private void Write()
        // {
        //     using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, fileSize,
        //         MemoryMappedFileAccess.ReadWrite);
        //
        //     var spinWait = new SpinWait();
        //     var remainSize = 0L;
        //     while (true)
        //     {
        //         if (queue.IsAddingCompleted && queue.Count == 0)
        //             break;
        //
        //         if (!queue.TryTake(out var chunk))
        //         {
        //             spinWait.SpinOnce();
        //             continue;
        //         }
        //
        //         spinWait = new SpinWait();
        //
        //         using var viewStream = memoryMappedFile.CreateViewStream(chunk.InitialOffset, chunk.Content.Length,
        //             MemoryMappedFileAccess.ReadWrite);
        //         chunk.Content.CopyTo(viewStream);
        //         remainSize -= chunk.Content.Length;
        //         chunk.Content.Dispose();
        //     }
        //
        //     if (remainSize > 0)
        //         throw new ArgumentException("Incorrect size");
        // }

        public void Dispose()
        {
            memoryMappedFile?.Dispose();
        }
    }
}