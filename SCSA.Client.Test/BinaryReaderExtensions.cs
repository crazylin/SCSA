using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Client.Test
{
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads a big endian (Motorola convention) unsigned 32-bit integer.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <returns>The unsigned integer.</returns>
        public static uint ReadBigEndianUInt32(this BinaryReader r)
        {
            byte[] a32 = r.ReadBytes(4);
            Array.Reverse(a32);
            return BitConverter.ToUInt32(a32, 0);
        }

        /// <summary>
        /// Reads a big endian (Motorola convention) signed 32-bit integer.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <returns>The signed integer.</returns>
        public static int ReadBigEndianInt32(this BinaryReader r)
        {
            byte[] a32 = r.ReadBytes(4);
            Array.Reverse(a32);
            return BitConverter.ToInt32(a32, 0);
        }

        /// <summary>
        /// Reads a big endian (Motorola convention) unsigned 16-bit integer.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <returns>The unsigned integer.</returns>
        public static ushort ReadBigEndianUInt16(this BinaryReader r)
        {
            byte[] a16 = r.ReadBytes(2);
            Array.Reverse(a16);
            return BitConverter.ToUInt16(a16, 0);
        }
        public static short ReadBigEndianInt16(this BinaryReader r)
        {
            byte[] a16 = r.ReadBytes(2);
            Array.Reverse(a16);
            return BitConverter.ToInt16(a16, 0);
        }
        public static float ReadBigEndianFloat(this BinaryReader r)
        {
            byte[] a = r.ReadBytes(4);
            Array.Reverse(a);
            return BitConverter.ToSingle(a, 0);
        }
        /// <summary>
        /// Reads a big endian (Motorola convention) 64-bit floating point number.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <returns>A <see cref="double" />.</returns>
        public static double ReadBigEndianDouble(this BinaryReader r)
        {
            byte[] a = r.ReadBytes(8);
            Array.Reverse(a);
            return BitConverter.ToDouble(a, 0);
        }
    }
}
