using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    internal sealed class FlicPacketParser : IDisposable
    {
        private const int FlicByteArraySize = 16;

        // A buffer used as a sink to dump remaining read bytes into
        private static readonly byte[] FlicByteArraySinkBuffer = new byte[FlicByteArraySize];

        private readonly BinaryReader backingReader;
        private bool disposedValue;

        internal FlicPacketParser(FlicPacket packet)
        {
            var stream = new ReadOnlyMemoryStream(packet.Data);
            backingReader = new BinaryReader(stream);
        }

        public bool IsComplete => backingReader.PeekChar() == -1;

        public uint ReadUInt32()
        {
            return backingReader.ReadUInt32();
        }

        public BluetoothAddress ReadBluetoothAddress()
        {
            var buffer = backingReader.ReadBytes(6);
            if (buffer.Length < 6)
                throw new EndOfStreamException();

            return new BluetoothAddress(buffer);
        }

        public byte ReadByte()
        {
            return backingReader.ReadByte();
        }

        public string ReadString(int length)
        {
            var buffer = length > 0 ? new byte[length] : Span<byte>.Empty;
            ReadBytes(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        public void ReadBytes(Span<byte> buffer)
        {
            int readBytes = backingReader.Read(buffer);
            if (readBytes < buffer.Length)
                throw new EndOfStreamException();

            int remainingBytes = FlicByteArraySize - readBytes;
            if (remainingBytes <= 0)
                return;

            backingReader.Read(FlicByteArraySinkBuffer[..remainingBytes]);
        }

        public sbyte ReadSByte()
        {
            return backingReader.ReadSByte();
        }

        public bool ReadBoolean()
        {
            return backingReader.ReadBoolean();
        }

        public ushort ReadUInt16()
        {
            return backingReader.ReadUInt16();
        }

        public short ReadInt16()
        {
            return backingReader.ReadInt16();
        }
        public byte[] ReadBytes(int count)
        {
            return backingReader.ReadBytes(count);
        }

        internal T ReadEnum<T>()
        {
            return (T)(object)backingReader.ReadByte();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    backingReader.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FlicPacketReader()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
