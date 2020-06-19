using System;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace GzipTest.Decompress
{
    public class DecompressWriteWorker
    {
        private readonly MemoryMappedFile memoryMappedFile;
        private readonly BlockingQueue<Chunk> chunks;
        private readonly Thread thread;
        private Bounder bounder;

        public DecompressWriteWorker(MemoryMappedFile memoryMappedFile, BlockingQueue<Chunk> chunks)
        {
            this.memoryMappedFile = memoryMappedFile;
            this.chunks = chunks;
            thread = new Thread(Write);
            bounder = new Bounder(8);
        }

        public void Start() => thread.Start();

        public void Wait() => thread.Join();

        private void Write()
        {
            while (chunks.TryTake(out var chunk))
            {
                bounder.WaitOne();
                var viewStream = memoryMappedFile.CreateViewStream(
                    chunk.InitialOffset,
                    chunk.Content.Length,
                    MemoryMappedFileAccess.ReadWrite
                );

                CopyStreamToStream(chunk.Content, viewStream, (x, y, z) =>
                {
                    x.Dispose();
                    y.Dispose();
                    bounder.ReleaseOne();
                });
            }
        }

        public static void CopyStreamToStream(
            Stream source, Stream destination,
            Action<Stream, Stream, Exception> completed)
        {
            byte[] buffer = new byte[0x1000];
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(null);

            Action<Exception> done = e =>
            {
                if (completed != null) asyncOp.Post(delegate { completed(source, destination, e); }, null);
            };

            AsyncCallback rc = null;
            rc = readResult =>
            {
                try
                {
                    int read = source.EndRead(readResult);
                    if (read > 0)
                    {
                        destination.BeginWrite(buffer, 0, read, writeResult =>
                        {
                            try
                            {
                                destination.EndWrite(writeResult);
                                source.BeginRead(
                                    buffer, 0, buffer.Length, rc, null);
                            }
                            catch (Exception exc)
                            {
                                done(exc);
                            }
                        }, null);
                    }
                    else done(null);
                }
                catch (Exception exc)
                {
                    done(exc);
                }
            };

            source.BeginRead(buffer, 0, buffer.Length, rc, null);
        }
    }
}