using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    public static class BinaryReaderExtension
    {
        public static Bdaddr ReadBdaddr(this BinaryReader reader)
        {
            var buffer = reader.ReadBytes(6);
            if (buffer.Length < 6)
                throw new EndOfStreamException();

            return new Bdaddr(buffer);
        }

        public static void Write(this BinaryWriter writer, Bdaddr bdaddr)
        {
            writer.Write(bdaddr.ToBytes());
        }
    }

    /// <summary>
    /// Represents a Bluetooth device address
    /// </summary>
    public readonly struct Bdaddr : IEquatable<Bdaddr>
    {
        public static readonly Bdaddr Blank = default;

        public Bdaddr(byte[] bytes)
        {
            if (bytes.Length != 6)
                throw new ArgumentException("Buffer too small", nameof(bytes));

            this.bytes = bytes;
        }

        private readonly byte[] bytes = new byte[6];

        public byte[] ToBytes() => bytes.AsSpan().ToArray();

        public static bool TryParse(string addr, out Bdaddr value)
        {
            try
            {
                value = Parse(addr);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public static Bdaddr Parse(string addr)
        {
            var bytes = new byte[6];

            bytes[5] = Convert.ToByte(addr.Substring(0, 2), 16);
            bytes[4] = Convert.ToByte(addr.Substring(3, 2), 16);
            bytes[3] = Convert.ToByte(addr.Substring(6, 2), 16);
            bytes[2] = Convert.ToByte(addr.Substring(9, 2), 16);
            bytes[1] = Convert.ToByte(addr.Substring(12, 2), 16);
            bytes[0] = Convert.ToByte(addr.Substring(15, 2), 16);

            return new Bdaddr(bytes);
        }

        /// <summary>
        /// The string representation of a Bluetooth device address (xx:xx:xx:xx:xx:xx)
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {
            return String.Format("{0:x2}:{1:x2}:{2:x2}:{3:x2}:{4:x2}:{5:x2}", bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0]);
        }

        public static bool operator ==(Bdaddr first, Bdaddr second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Bdaddr first, Bdaddr second)
        {
            return !(first == second);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is Bdaddr bdaddr)
                return Equals(bdaddr);

            return base.Equals(obj);
        }

        public readonly bool Equals(Bdaddr other)
        {
            return bytes.AsSpan().SequenceEqual(other.bytes.AsSpan());
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                var hc = new HashCode();
                hc.AddBytes(bytes.AsSpan());
                int hashCode = hc.ToHashCode();
                return hashCode;
            }
        }
    }
}
