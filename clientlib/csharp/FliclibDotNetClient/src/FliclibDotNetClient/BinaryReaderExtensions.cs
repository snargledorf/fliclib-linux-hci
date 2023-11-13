using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FliclibDotNetClient
{
    public static class BinaryReaderExtensions
    {
        private const int FlicArraySize = 16;

        // A buffer used as a sink to dump remaining read bytes into
        private static readonly byte[] FlicArraySinkBuffer = new byte[FlicArraySize];

        public static Bdaddr ReadBdaddr(this BinaryReader reader)
        {
            var buffer = reader.ReadBytes(6);
            if (buffer.Length < 6)
                throw new EndOfStreamException();

            return new Bdaddr(buffer);
        }

        public static string ReadFlicString(this BinaryReader reader, int length)
        {
            var buffer = length > 0 ? new byte[length] : Span<byte>.Empty;
            ReadFlicArray(reader, buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        public static void ReadFlicArray(this BinaryReader reader, Span<byte> buffer)
        {
            int readBytes = reader.Read(buffer);
            if(readBytes < buffer.Length)
                throw new EndOfStreamException();
            
            int remainingBytes = FlicArraySize - readBytes;
            if (remainingBytes <= 0)
                return;

            reader.Read(FlicArraySinkBuffer[..remainingBytes]);
        }

        public static void Write(this BinaryWriter writer, Bdaddr bdaddr)
        {
            writer.Write(bdaddr.ToBytes());
        }
    }
}
