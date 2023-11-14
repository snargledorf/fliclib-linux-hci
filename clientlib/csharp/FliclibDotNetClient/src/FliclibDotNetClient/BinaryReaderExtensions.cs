using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FliclibDotNetClient
{
    public static class BinaryReaderExtensions
    {
        public static void Write(this BinaryWriter writer, Bdaddr bdaddr)
        {
            writer.Write(bdaddr.ToBytes());
        }
    }
}
