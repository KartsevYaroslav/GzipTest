using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest.Decompress
{
    public class DecompressWriteWorker
    {
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly BlockingBag<Chunk> chunks;
        private readonly Thread thread;

        public DecompressWriteWorker(MemoryMappedFile memoryMappedFile, BlockingBag<Chunk> chunks)
        {
            this.memoryMappedFile = memoryMappedFile;
            this.chunks = chunks;
            thread = new Thread(Write);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Wait()
        {
            thread.Join();
        }

        private void Write()
        {
            var spinWait = new SpinWait();
            while (true)
            {
                if (chunks.IsAddingCompleted && chunks.Count == 0)
                    break;

                if (!chunks.TryTake(out var chunk))
                {
                    spinWait.SpinOnce();
                    continue;
                }

                spinWait = new SpinWait();

                using var viewStream = memoryMappedFile.CreateViewStream(chunk.InitialOffset, chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite);
                chunk.Content.CopyTo(viewStream);
                chunk.Content.Dispose();
                viewStream.Dispose();
            }
        }
    }
}