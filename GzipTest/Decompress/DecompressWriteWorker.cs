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

        public void Start() => thread.Start();

        public void Wait() => thread.Join();

        private void Write()
        {
            while (chunks.TryTake(out var chunk))
            {
                using var viewStream = memoryMappedFile.CreateViewStream(
                    chunk.InitialOffset,
                    chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite
                    );
                
                chunk.Content.CopyTo(viewStream);
                chunk.Content.Dispose();
            }
        }
    }
}