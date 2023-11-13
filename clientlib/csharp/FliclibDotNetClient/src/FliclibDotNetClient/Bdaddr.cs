using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{

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
            if (addr.Length != 17)
                throw new FormatException("Invaid address format");

            var parseBuffer = new byte[6];

            for (int byteIndex = 5, addrIndex = 0; byteIndex >= 0; byteIndex--, addrIndex+=3)
                parseBuffer[byteIndex] = byte.Parse(addr.Substring(addrIndex, 2), NumberStyles.HexNumber);

            return new Bdaddr(parseBuffer);
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
            var hc = new HashCode();
            hc.AddBytes(bytes.AsSpan());
            return hc.ToHashCode();
        }
    }
}
