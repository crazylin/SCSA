using System.IO.Hashing;
using SCSA.IO.Net.TCP;

namespace SCSA.Models;

public class NetDataPackage : INetDataPackage
{
    public byte[] Header { set; get; } = new byte[] { 0x53, 0x43, 0x5A, 0x4E };
    public byte Version { set; get; }
    public DeviceCommand DeviceCommand { get; set; }

    public short Flag { set; get; }
    public int DataLen { set; get; }

    public byte[] Data { set; get; }

    public byte[] Crc { set; get; }

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

        Header = header;
        Version = version;
        DeviceCommand = (DeviceCommand)command;
        Flag = flag;
        DataLen = len;
        Data = data;
        Crc = crc;


        //var bytes = Get();
        //Debug.WriteLine($"Server Recv: {bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");
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
        DataLen = Data is { Length: > 0 } ? Data.Length : 0;
        bytes.AddRange(BitConverter.GetBytes(DataLen));
        if (Data is { Length: > 0 }) bytes.AddRange(Data);

        Crc = BitConverter.GetBytes(Crc32.HashToUInt32(bytes.ToArray()));
        //添加CRC校验
        bytes.AddRange(Crc);


        //Debug.WriteLine($"Build: {bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");

        return bytes.ToArray();
    }

    public override string ToString()
    {
        //var data = Data != null ? Data.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n) : "null";
        return
            $"CMD: {DeviceCommand} DataLen {DataLen} Crc {Crc.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}";
    }
}