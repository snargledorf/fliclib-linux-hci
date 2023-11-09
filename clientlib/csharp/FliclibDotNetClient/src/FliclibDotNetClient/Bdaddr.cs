using System;
using System.Collections.Generic;
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

        private readonly byte[] _bytes = new byte[6];

        public Bdaddr()
        {
        }

        public Bdaddr(byte[] bytes)
        {
            _bytes = bytes;
        }

        internal Bdaddr(BinaryReader reader)
            : this()
        {
            int read = reader.Read(_bytes);
            if (read != 6)
                throw new EndOfStreamException();
        }

        internal readonly void WriteBytes(BinaryWriter writer)
        {
            writer.Write(_bytes);
        }

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
            return String.Format("{0:x2}:{1:x2}:{2:x2}:{3:x2}:{4:x2}:{5:x2}", _bytes[5], _bytes[4], _bytes[3], _bytes[2], _bytes[1], _bytes[0]);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is Bdaddr bdaddr)
                return Equals(bdaddr);

            return base.Equals(obj);
        }

        public static bool operator ==(Bdaddr first, Bdaddr second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Bdaddr first, Bdaddr second)
        {
            return !(first == second);
        }

        public readonly bool Equals(Bdaddr other)
        {
            return Equals(_bytes, other._bytes);
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hashCode = 47;
                if (_bytes != null)
                    hashCode = (hashCode * 53) ^ EqualityComparer<byte[]>.Default.GetHashCode(_bytes);

                return hashCode;
            }
        }
    }
}
