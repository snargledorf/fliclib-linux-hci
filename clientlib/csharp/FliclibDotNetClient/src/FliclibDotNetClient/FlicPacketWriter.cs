using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    internal class FlicPacketWriter
    {
        private const int HeaderOpCodeSizeBytes = 1;
        private const int HeaderDataLengthSizeBytes = 2;

        private readonly Stream backingStream;

        public FlicPacketWriter(Stream stream)
        {
            backingStream = stream;
        }

        public ValueTask WritePacketAsync(FlicPacket packet, CancellationToken cancellationToken)
        {
            var packetSizeIncludeOpCode = (short)(HeaderOpCodeSizeBytes + packet.Data.Length);

            Memory<byte> buffer = new byte[HeaderDataLengthSizeBytes + packetSizeIncludeOpCode];
            Span<byte> bufferSpan = buffer.Span;

            BitConverter.GetBytes(packetSizeIncludeOpCode).CopyTo(bufferSpan);

            bufferSpan[2] = packet.OpCode;

            packet.Data.CopyTo(bufferSpan[3..]);

            return backingStream.WriteAsync(buffer, cancellationToken);
        }
    }
}
