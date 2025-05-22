using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Utils
{
    public static class BitConverterExtensions
    {
        public static byte[] GetBigEndianBytes(Int16 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }
        public static byte[] GetBigEndianBytes(UInt16 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }
        public static byte[] GetBigEndianBytes(Int32 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }
        public static byte[] GetBigEndianBytes(UInt32 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }
        public static byte[] GetBigEndianBytes(Int64 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }
        public static byte[] GetBigEndianBytes(UInt64 v)
        {
            var b = BitConverter.GetBytes(v);
            Array.Reverse(b);
            return b;
        }

        public static UInt32 ToBigEndianUInt32(byte[] b)
        {
            Array.Reverse(b);
            return BitConverter.ToUInt32(b);
        }
        public static Int32 ToBigEndianInt32(byte[] b)
        {
            Array.Reverse(b);
            return BitConverter.ToInt32(b);
        }
        public static UInt16 ToBigEndianUInt16(byte[] b)
        {
            Array.Reverse(b);
            return BitConverter.ToUInt16(b);
        }
        public static Int16 ToBigEndianInt16(byte[] b)
        {
            Array.Reverse(b);
            return BitConverter.ToInt16(b);
        }
    }
}
