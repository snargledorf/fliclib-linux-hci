using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    internal class FlicStreamReader
    {
        private readonly byte[] lengthReadBytes = new byte[2];

        private readonly Stream backingStream;

        public FlicStreamReader(Stream stream)
        {
            backingStream = stream ?? throw new ArgumentNullException(nameof(stream), $"{nameof(stream)} is null.");
        }

        public async Task<FlicPacket> ReadPacketAsync(CancellationToken cancellationToken = default)
        {
            Memory<byte> lengthReadBuffer = lengthReadBytes;
            int bytesRead = await ReadBufferAsync(lengthReadBuffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead < lengthReadBuffer.Length)
                return FlicPacket.None;

            short packetLength = BitConverter.ToInt16(lengthReadBytes.AsSpan());

            byte[] packetBytes = ArrayPool<byte>.Shared.Rent(packetLength);
            Memory<byte> packetBuffer = packetBytes[..packetLength];

            try
            {
                bytesRead = await ReadBufferAsync(packetBuffer, cancellationToken).ConfigureAwait(false);
                
                // Sanity check: This should never happen, but put a guard just in case
                if (bytesRead > packetLength)
                    throw new InvalidDataException("Too many bytes read from stream");
                
                // Can happen if the stream ends abruptly
                if (bytesRead < packetLength)
                    return FlicPacket.None;

                return new FlicPacket(packetBuffer.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packetBytes);
            }
        }

        private async Task<int> ReadBufferAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int bufferLength = buffer.Length;
            while (!buffer.IsEmpty)
            {
                int bytesRead = await backingStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                    return 0;

                buffer = buffer[bytesRead..];
            }

            return bufferLength;
        }
    }
}
