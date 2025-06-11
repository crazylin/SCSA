using System.Buffers;

namespace SCSA.IO.Net.TCP;

/// <summary>
///     管道模式下的数据包接口：它负责在“可读缓冲区”里尝试拆包，
///     如果拿到一个完整帧，就输出一个 T 实例，并告知“拆包消费到什么位置”。
/// </summary>
public interface IPipelineDataPackage<T> where T : class, new()
{
    /// <summary>
    ///     在 buffer（可能跨多个 segment）里尝试找到一个完整帧。
    ///     如果找到：
    ///     1. 把解析得到的“完整 T” 赋给 out packet；
    ///     2. 把“本帧结束位置”赋给 out frameEnd（相对 buffer 的 SequencePosition）；
    ///     3. 返回 true。
    ///     如果未找到完整帧（半包或长度不够），返回 false，packet 和 frameEnd 无需赋值（默认）。
    ///     如果遇到“Magic 对不起来”或“长度异常”或“CRC 校验失败”，会跳过相应字节并递归 / 循环重试。
    ///     由使用者在 `PipeReader` 循环逻辑中决定何时调用 AdvanceTo(consumed, examined)。
    /// </summary>
    bool TryParse(ReadOnlySequence<byte> buffer, out T packet, out SequencePosition frameEnd);
}

/// <summary>
///     仅用于 SendAsync 时，把 T 转成字节数组发出
/// </summary>
public interface IPacketWritable
{
    /// <summary>
    ///     返回一个完整的字节数组，包含 Magic/Ver/Cmd/CmdId/Len/Body/CRC
    /// </summary>
    byte[] GetBytes();
}