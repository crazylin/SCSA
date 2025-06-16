using System;

namespace SCSA.Utils;

/// <summary>
/// 线程不安全的固定容量环形缓冲区，写入 O(1)；溢出时自动覆盖最旧数据。
/// 通过 ToArray / CopyTo 可一次性取出连续数据快照。
/// </summary>
public sealed class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head; // 下一个写入位置

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new T[capacity];
        _head = 0;
        Count = 0;
    }

    /// <summary>当前有效元素数量（≤ Capacity）。</summary>
    public int Count { get; private set; }

    /// <summary>缓冲区容量。</summary>
    public int Capacity => _buffer.Length;

    /// <summary>追加一个元素，满时覆盖最旧数据。</summary>
    public void Add(T value)
    {
        _buffer[_head] = value;
        _head = (_head + 1) % Capacity;
        if (Count < Capacity)
            Count++;
    }

    /// <summary>取出所有有效元素（从旧到新）并复制到新数组。</summary>
    public T[] ToArray()
    {
        var dest = new T[Count];
        CopyTo(dest);
        return dest;
    }

    /// <summary>将缓冲区内容（从旧到新）复制到已有数组，长度以两者较小者为准。</summary>
    public void CopyTo(T[] dest)
    {
        if (dest == null) throw new ArgumentNullException(nameof(dest));
        var len = Math.Min(dest.Length, Count);
        if (len == 0) return;

        // tail 指向最旧元素
        int tail = (_head - Count + Capacity) % Capacity;
        if (tail + len <= Capacity)
        {
            Array.Copy(_buffer, tail, dest, 0, len);
        }
        else
        {
            int firstPart = Capacity - tail;
            Array.Copy(_buffer, tail, dest, 0, firstPart);
            Array.Copy(_buffer, 0, dest, firstPart, len - firstPart);
        }
    }
} 