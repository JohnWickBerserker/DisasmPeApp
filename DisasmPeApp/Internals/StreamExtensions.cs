using DisasmPeApp.Exceptions;
using System;
using System.IO;
using System.Text;

namespace DisasmPeApp.Internals
{
    internal static class StreamExtensions
    {
        public static uint ReadUint(this Stream stream)
        {
            var b = new byte[4];
            var ret = stream.Read(b, 0, 4);
            if (ret != 4)
            {
                throw new Exception("Could not read");
            }
            return ((uint)b[3] << 24) | ((uint)b[2] << 16) | ((uint)b[1] << 8) | ((uint)b[0]);
        }

        public static ushort ReadUshort(this Stream stream)
        {
            var b = new byte[2];
            var ret = stream.Read(b, 0, 2);
            if (ret != 2)
            {
                throw new Exception("Could not read");
            }
            return (ushort)(((ushort)b[1] << 8) | ((ushort)b[0]));
        }

        public static string ReadString(this Stream stream)
        {
            var sb = new StringBuilder();
            var b = stream.ReadByte();
            while (b != 0)
            {
                if (b == -1)
                {
                    throw new NotPeFileException();
                }
                sb.Append((char)b);
                b = stream.ReadByte();
            }
            return sb.ToString();
        }
    }
}
