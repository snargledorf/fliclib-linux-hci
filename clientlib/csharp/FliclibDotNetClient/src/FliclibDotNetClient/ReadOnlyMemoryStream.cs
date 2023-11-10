using System;
using System.IO;

namespace FliclibDotNetClient
{
    internal class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> buffer;
        private ReadOnlyMemory<byte> readBuffer;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> buffer)
        {
            this.buffer = buffer;
            readBuffer = buffer;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => readBuffer.Length;

        public override long Position
        {
            get => buffer.Length - readBuffer.Length;
            
            set
            {
                if (value > buffer.Length || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                readBuffer = buffer[(int)value..];
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readBytes = Math.Min(readBuffer.Length, count);

            readBuffer.Span[..readBytes].CopyTo(buffer.AsSpan(offset, readBytes));
            readBuffer = readBuffer[readBytes..];

            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin.HasFlag(SeekOrigin.Begin))
                Position = offset;
            else if (origin.HasFlag(SeekOrigin.Current))
                Position += offset;
            else if (origin.HasFlag(SeekOrigin.End))
                Position = buffer.Length - offset;

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
