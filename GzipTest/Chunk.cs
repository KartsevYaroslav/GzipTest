using System;
using System.IO;

namespace GzipTest
{
    public class Chunk
    {
        public Chunk(long initialOffset, Stream content)
        {
            InitialOffset = initialOffset;
            Content = content;
        }

        public long InitialOffset { get; }
        public Stream Content { get;}

        // public ReadOnlyMemory<byte> ToBytes()
        // {
        //     Memory<byte> buffer = new byte[Content.Length + 8];
        //     BitConverter.TryWriteBytes(buffer.Span, InitialOffset);
        //     Content.CopyTo(buffer.Slice(7));
        //     
        //     return buffer;
        // }
    }
}