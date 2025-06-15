using System.Buffers;
using System.IO.Hashing;
using SCSA.IO.Net.TCP;

namespace SCSA.Models;

public class PipelineNetDataPackage : IPipelineDataPackage<PipelineNetDataPackage>, IPacketWritable
{
    private const uint EXPECTED_MAGIC = 'S' | ('C' << 8) | ('Z' << 16) | ('N' << 24);
    public byte Version { set; get; }
    public DeviceCommand DeviceCommand { get; set; }

    public short CmdId { set; get; }
    public int DataLen { set; get; }

    public byte[] Data { set; get; }

    public uint Crc { set; get; }


    /// <summary>
    ///     写包：拼接 Magic/Ver/Cmd/CmdId/Len/Body/CRC
    /// </summary>
    public byte[] GetBytes()
    {
        var bodyLen = Data?.Length ?? 0;
        var totalLen = 4 // Magic
                       + 1 // Version
                       + 1 // Command
                       + 2 // CmdId
                       + 4 // BodyLength
                       + bodyLen
                       + 4; // CRC32

        var buffer = new byte[totalLen];
        var idx = 0;

        // 1) Magic (UInt32)，数值 0xAABBCCDD，小端写：低位先写
        //    0xAABBCCDD 拆成四字节： 0xDD, 0xCC, 0xBB, 0xAA

        buffer[idx++] = (byte)(EXPECTED_MAGIC & 0xFF);
        buffer[idx++] = (byte)((EXPECTED_MAGIC >> 8) & 0xFF);
        buffer[idx++] = (byte)((EXPECTED_MAGIC >> 16) & 0xFF);
        buffer[idx++] = (byte)((EXPECTED_MAGIC >> 24) & 0xFF);

        // 2) Version (1 byte)
        buffer[idx++] = Version;

        // 3) Command (1 byte)
        buffer[idx++] = (byte)DeviceCommand;

        // 4) CmdId (UInt16，小端)
        buffer[idx++] = (byte)(CmdId & 0xFF);
        buffer[idx++] = (byte)((CmdId >> 8) & 0xFF);

        // 5) BodyLength (Int32，小端)
        buffer[idx++] = (byte)(bodyLen & 0xFF);
        buffer[idx++] = (byte)((bodyLen >> 8) & 0xFF);
        buffer[idx++] = (byte)((bodyLen >> 16) & 0xFF);
        buffer[idx++] = (byte)((bodyLen >> 24) & 0xFF);

        // 6) Body (如果有)
        if (bodyLen > 0)
        {
            Array.Copy(Data, 0, buffer, idx, bodyLen);
            idx += bodyLen;
        }

        // 7) CRC32 (UInt32，小端)，CRC 计算范围为“从第 0 个字节到 Body 末尾”共 (totalLen-4) 字节
        //uint crc = Crc32Helper.Compute(buffer, 0, idx);
        //Crc = Crc32.CRC32_Check_T(buffer, (uint)buffer.Length);
        Crc = Crc32.HashToUInt32(buffer.Take(idx).ToArray());

        buffer[idx++] = (byte)(Crc & 0xFF);
        buffer[idx++] = (byte)((Crc >> 8) & 0xFF);
        buffer[idx++] = (byte)((Crc >> 16) & 0xFF);
        buffer[idx++] = (byte)((Crc >> 24) & 0xFF);

        return buffer;
    }

    public bool TryParse(ReadOnlySequence<byte> buffer, out PipelineNetDataPackage packet,
        out SequencePosition frameEnd)
    {
        packet = null;
        frameEnd = buffer.Start;

        // 1) 检查“最少整体长度” （Magic 4 + Ver1 + Cmd1 + CmdId2 + BodyLen4 + CRC4 = 16 字节）
        if (buffer.Length < 16)
            return false;

        // 2) 用 SequenceReader 来跨段读取头部字段
        var reader = new SequenceReader<byte>(buffer);

        // 2.1) 先读 Magic（UInt32，小端），如果不够 4 字节会返回 false
        if (!reader.TryReadLittleEndian(out int magicInBuf))
            return false; // 虽然 buffer.Length >= 16，但可能第一笔数据不足 4 字节先退出，下次继续

        if (magicInBuf != EXPECTED_MAGIC)
        {
            // 找不到 Magic，就要跳过 1 字节，然后重试
            // 注意：SequenceReader.PrefixLength 是当前 Segment 开始到 reader.Consumed 的偏移
            var nextPosition = buffer.GetPosition(1, buffer.Start);
            return TryParse(buffer.Slice(nextPosition), out packet, out frameEnd);
        }

        // 2.2) 读 Version (1 字节)
        if (!reader.TryRead(out var version))
            return false;

        // 2.3) 读 Command (1 字节)
        if (!reader.TryRead(out var command))
            return false;

        // 2.4) 读 CmdId (UInt16，小端)
        if (!reader.TryReadLittleEndian(out short cmdIdUShort))
            return false;
        var cmdId = cmdIdUShort;

        // 2.5) 读 BodyLen (Int32，小端)
        if (!reader.TryReadLittleEndian(out int bodyLen))
            return false;
        if (bodyLen < 0 || bodyLen > 8 * 1024 * 1024)
        {
            // 长度异常，跳过一个字节重新拆
            var nextPosition = buffer.GetPosition(1, buffer.Start);
            return TryParse(buffer.Slice(nextPosition), out packet, out frameEnd);
        }

        // 3) 计算整帧长度
        var totalFrameLen = 12L + bodyLen + 4L; // 12 = 4(Magic)+1+1+2+4
        if (buffer.Length < totalFrameLen)
            return false; // 整帧还没完全到齐，下次再来

        // 4) 拆出 Payload：从头部结束位置往后  bodyLen 字节
        //    注意 SequenceReader 已经走到了“读完 BodyLen 字段”的位置
        //    但我们需要一个方便的方式直接用原始 buffer 来切片：
        var payloadStart = buffer.GetPosition(12, buffer.Start); // 从帧头开始算，偏移 12 就是 payload 起点
        var payloadSeq = buffer.Slice(payloadStart, bodyLen);

        // 5) 读 CRC：再向后 4 字节
        var crcStart = buffer.GetPosition(12 + bodyLen, buffer.Start);
        Span<byte> crcSpan = stackalloc byte[4];
        buffer.Slice(crcStart, 4).CopyTo(crcSpan);
        var receivedCrc = (uint)(
            crcSpan[0]
            | (crcSpan[1] << 8)
            | (crcSpan[2] << 16)
            | (crcSpan[3] << 24)
        );

        // 6) 计算 CRC 校验（小端），针对“Magic 到 payload 末尾”这 12 + bodyLen 字节
        //Span<byte> checkSpan = stackalloc byte[(int)(12 + bodyLen)];
        //buffer.Slice(0, 12 + bodyLen).CopyTo(checkSpan);
        //uint computedCrc = Crc32.CRC32_Check_T(checkSpan, (uint)checkSpan.Length);
        //if (computedCrc != receivedCrc)
        //{
        //    // CRC 校验失败：跳过这一整帧长度后重试
        //    var skipPos = buffer.GetPosition(totalFrameLen, buffer.Start);
        //    return TryParse(buffer.Slice(skipPos), out packet, out frameEnd);
        //}

        // 7) 构造数据包
        packet = new PipelineNetDataPackage
        {
            Version = version,
            DeviceCommand = (DeviceCommand)command,
            CmdId = cmdId,
            DataLen = bodyLen,
            Data = payloadSeq.ToArray(),
            Crc = receivedCrc
        };

        // 8) 计算帧结束的 SequencePosition（相对于原 buffer.Start 偏移 totalFrameLen）
        frameEnd = buffer.GetPosition(totalFrameLen, buffer.Start);
        return true;
    }
}