using SCSA.IO.Net.TCP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Client.Test
{
    public class NetDataPackage : INetDataPackage
    {
        public byte[] Header { set; get; } = new byte[] { 0x53, 0x43, 0x5A, 0x4E };
        public byte Version { set; get; } = 0x00;
        public DeviceCommand DeviceCommand { get; set; }

        public Int16 Flag { set; get; }
        public Int32 DataLen { set; get; }

        public byte[] Data { set; get; }

        public byte[] Crc { set; get; }

        public override string ToString()
        {
            var data = Data != null ? Data.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n) : "null";
            return
                $"CMD: {DeviceCommand} DataLen {DataLen} Data {data} Crc {Crc.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}";
        }

        public byte[] RawData { get; set; }

        public void Read(BinaryReader reader)
        {
            var header = reader.ReadBytes(4);
            var version = reader.ReadByte();
            var command = reader.ReadByte();
            var flag = reader.ReadInt16();
            var len = reader.ReadInt32();
            var data = reader.ReadBytes(len);
            var crc = reader.ReadBytes(4);

            this.Header = header;
            this.Version = version;
            this.DeviceCommand = (DeviceCommand)command;
            this.Flag = flag;
            this.DataLen = len;
            this.Data = data;
            this.Crc = crc;

            var bytes = Get();
            //Debug.WriteLine($"Client Recv: {bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");
        }

        public byte[] Get()
        {
            var bytes = new List<byte>();

            //添加命令
            bytes.AddRange(Header);
            bytes.Add(Version);
            bytes.Add((byte)DeviceCommand);
            bytes.AddRange(BitConverter.GetBytes(Flag));
            //添加数据区长
            bytes.AddRange(BitConverter.GetBytes(Data is { Length: > 0 } ? Data.Length : 0));
            if (Data is { Length: > 0 })
            {
                bytes.AddRange(Data);
            }

            Crc = BitConverter.GetBytes(Crc32.CRC32_Check_T(bytes.ToArray(), (uint)bytes.Count));
            //添加CRC校验
            bytes.AddRange(Crc);
            return bytes.ToArray();
        }

    }
}
