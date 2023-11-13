using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FliclibDotNetClient
{
    internal static class FlicIdGenerator<T>
    {
        private static uint nextId;

        public static uint NextId()
        {
            return Interlocked.Increment(ref nextId);
        }
    }
}
