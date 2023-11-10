using System;
using System.Collections.Generic;

namespace FliclibDotNetClient
{
    internal struct FlicPacket : IEquatable<FlicPacket>
    {
        public static readonly FlicPacket None = default;

        public FlicPacket(byte opCode, ReadOnlySpan<byte> data)
        {
            OpCode = opCode;
            Data = data.ToArray();
        }

        public FlicPacket(Span<byte> packetBytes)
        {
            OpCode = packetBytes[0];
            Data = packetBytes[1..].ToArray();
        }

        public byte OpCode { get; }

        public byte[] Data { get; }

        public static bool operator ==(FlicPacket first, FlicPacket second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(FlicPacket first, FlicPacket second)
        {
            return !(first == second);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is FlicPacket flicPacket)
            {
                return Equals(flicPacket);
            }

            return base.Equals(obj);
        }

        public readonly bool Equals(FlicPacket other)
        {
            if (!OpCode.Equals(other.OpCode))
                return false;

            if (Data == null && other.Data == null)
                return true;

            return Data is byte[] myData
                && other.Data is byte[] otherData
                && myData.AsSpan().SequenceEqual(otherData.AsSpan());
        }

        public override readonly int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(OpCode);
            if (Data is byte[] myData)
                hc.AddBytes(myData.AsSpan());
            return hc.ToHashCode();
        }
    }
}
