using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SCSA.IO.Net.TCP;

namespace SCSA.Models
{
    public class PipelineNetDataPackage : IPipelineDataPackage<PipelineNetDataPackage>, IPacketWritable
    {
        public byte Version { set; get; } = 0x00;
        public DeviceCommand DeviceCommand { get; set; }

        public Int16 CmdId { set; get; }
        public Int32 DataLen { set; get; }

        public byte[] Data { set; get; }

        public byte[] Crc { set; get; }


        //public byte Version { get; private set; }
        //public byte Command { get; private set; }
        //public ushort CmdId { get; private set; }
        //public byte[] Body { get; private set; }

        public PipelineNetDataPackage()
        {
        }

        /// <summary>
        /// 写包：拼接 Magic/Ver/Cmd/CmdId/Len/Body/CRC
        /// </summary>
        public byte[] GetBytes()
        {
            int bodyLen = Data?.Length ?? 0;
            int totalLen = 4    // Magic
                         + 1    // Version
                         + 1    // Command
                         + 2    // CmdId
                         + 4    // BodyLength
                         + bodyLen
                         + 4;   // CRC32

            byte[] buffer = new byte[totalLen];
            int idx = 0;

            // 1) Magic (UInt32)，数值 0xAABBCCDD，小端写：低位先写
            //    0xAABBCCDD 拆成四字节： 0xDD, 0xCC, 0xBB, 0xAA
           
            buffer[idx++] = 0x53;
            buffer[idx++] = 0x43;
            buffer[idx++] = 0x5A;
            buffer[idx++] = 0x4E;

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
            uint crc = Crc32Helper.Compute(buffer, 0, idx);
            buffer[idx++] = (byte)(crc & 0xFF);
            buffer[idx++] = (byte)((crc >> 8) & 0xFF);
            buffer[idx++] = (byte)((crc >> 16) & 0xFF);
            buffer[idx++] = (byte)((crc >> 24) & 0xFF);

            return buffer;
        }

        /// <summary>
        /// 解析
        /// </summary>
        public bool TryParse(ReadOnlySequence<byte> buffer, out PipelineNetDataPackage packet, out SequencePosition frameEnd)
        {
            packet = null;
            frameEnd = buffer.Start;

            // 1) 最少要 16 字节：4B Magic + 1B Ver + 1B Cmd + 2B CmdId + 4B BodyLen + 4B CRC
            if (buffer.Length < 16)
                return false;

            // 2) 为简化，对“跨 segment”情况先不专门处理头 12 字节，
            //    只在 buffer.First.Span 长度 >=12 时再尝试解析头部。否则返回 false 等下次。
            var firstSpan = buffer.First.Span;
            if (firstSpan.Length < 12)
                return false;

            // 3) 读取 Magic（UInt32，小端）
            //    firstSpan[0] 是最低位；[3] 是最高位
            uint magicInBuf = (uint)(
                (firstSpan[0] << 24)
                | (firstSpan[1] << 16)
                | (firstSpan[2] << 8)
                | (firstSpan[3])
            );
            const uint EXPECTED_MAGIC = 0x53435A4E;
            if (magicInBuf != EXPECTED_MAGIC)
            {
                // Magic 不匹配：跳过 1 字节，重试
                buffer = buffer.Slice(1);
                return TryParse(buffer, out packet, out frameEnd);
            }

            // 4) 读取 Version(1B)、Command(1B)、CmdId(2B, 小端)、BodyLength(4B, 小端)
            byte version = firstSpan[4];
            byte command = firstSpan[5];

            short cmdId = (short)(
                 (firstSpan[6])
               | (firstSpan[7] << 8)
            );

            int bodyLen =
                 (firstSpan[8])
               | (firstSpan[9] << 8)
               | (firstSpan[10] << 16)
               | (firstSpan[11] << 24);
            if (bodyLen < 0 || bodyLen > 8 * 1024 * 1024)
            {
                // 长度异常：跳过 1 字节重试
                buffer = buffer.Slice(1);
                return TryParse(buffer, out packet, out frameEnd);
            }

            // 5) 计算整帧长度：12（头） + bodyLen + 4（CRC）
            long totalFrameLen = 12L + bodyLen + 4L;
            if (buffer.Length < totalFrameLen)
                return false; // 半包，等下次

            // 6) 拆 Payload 和 CRC
            //    6.1) Payload 部分可能跨多个 segment，直接用 Slice
            var payloadSeq = buffer.Slice(12, bodyLen);

            //    6.2) 读取对端发来的 CRC（4B，小端）
            Span<byte> crcSpan = stackalloc byte[4];
            buffer.Slice(12 + bodyLen, 4).CopyTo(crcSpan);
            uint receivedCrc = (uint)(
                 (crcSpan[0])
               | (crcSpan[1] << 8)
               | (crcSpan[2] << 16)
               | (crcSpan[3] << 24)
            );

            // 7) 计算 CRC32（针对“Magic 到 Payload 末尾”这 12+bodyLen 字节，都是小端排列下的原始数据）
            int headerPlusBody = 12 + bodyLen;
            Span<byte> checkSpan = stackalloc byte[headerPlusBody];
            buffer.Slice(0, headerPlusBody).CopyTo(checkSpan);
            uint computedCrc = Crc32Helper.Compute(checkSpan, 0, checkSpan.Length);

            //暂时不判断
            //if (computedCrc != receivedCrc)
            //{
            //    // CRC 校验失败：跳过整个“totalFrameLen”字节后，递归重试
            //    buffer = buffer.Slice(totalFrameLen);
            //    return TryParse(buffer, out packet, out frameEnd);
            //}

            // 8) 校验通过，构造一个 PipelineNetDataPackage
            packet = new PipelineNetDataPackage
            {
                Version = version,
                DeviceCommand = (DeviceCommand)command,
                CmdId = cmdId,
                Data = payloadSeq.ToArray() // 如果不想拷贝，可以把 Sequence<byte> 传给业务层；这里简化用 ToArray()
            };

            // 9) 计算出本帧“结束位置”的 SequencePosition
            frameEnd = buffer.GetPosition(totalFrameLen);

            return true;
        }

        private static class Crc32Helper
        {
            private static readonly uint[] Table;

            static Crc32Helper()
            {
                uint poly = 0x04C11DB7;
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint crc = i << 24;
                    for (int j = 0; j < 8; j++)
                    {
                        if ((crc & 0x80000000) != 0)
                            crc = (crc << 1) ^ poly;
                        else
                            crc <<= 1;
                    }

                    Table[i] = crc;
                }
            }

            public static uint Compute(ReadOnlySpan<byte> data, int offset, int length)
            {
                uint crc = 0xFFFFFFFF;
                for (int i = 0; i < length; i++)
                {
                    byte b = data[offset + i];
                    byte idx = (byte)((crc >> 24) ^ b);
                    crc = (crc << 8) ^ Table[idx];
                }

                return crc ^ 0xFFFFFFFF;
            }
        }

    }
}
