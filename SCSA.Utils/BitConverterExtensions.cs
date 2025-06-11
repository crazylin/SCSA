namespace SCSA.Utils;

public static class BitConverterExtensions
{
    public static byte[] GetBigEndianBytes(short v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static byte[] GetBigEndianBytes(ushort v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static byte[] GetBigEndianBytes(int v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static byte[] GetBigEndianBytes(uint v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static byte[] GetBigEndianBytes(long v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static byte[] GetBigEndianBytes(ulong v)
    {
        var b = BitConverter.GetBytes(v);
        Array.Reverse(b);
        return b;
    }

    public static uint ToBigEndianUInt32(byte[] b)
    {
        Array.Reverse(b);
        return BitConverter.ToUInt32(b);
    }

    public static int ToBigEndianInt32(byte[] b)
    {
        Array.Reverse(b);
        return BitConverter.ToInt32(b);
    }

    public static ushort ToBigEndianUInt16(byte[] b)
    {
        Array.Reverse(b);
        return BitConverter.ToUInt16(b);
    }

    public static short ToBigEndianInt16(byte[] b)
    {
        Array.Reverse(b);
        return BitConverter.ToInt16(b);
    }
}