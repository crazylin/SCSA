using System.IO;

namespace SCSA.Services.Recording.Writers;

public enum BinDataType
{
    Float32,
    Int16
}

/// <summary>
///     将采样数据写入 .bin 文件，支持 float32 或 int16 格式。
/// </summary>
internal sealed class BinChannelWriter : IChannelWriter
{
    private readonly BinDataType _dataType;
    private readonly BinaryWriter _writer;
    private bool _disposed;

    public BinChannelWriter(BinaryWriter writer, BinDataType dataType)
    {
        _writer = writer;
        _dataType = dataType;
    }

    public void Write(double[] samples)
    {
        if (_disposed) return;

        switch (_dataType)
        {
            case BinDataType.Int16:
                foreach (var v in samples) _writer.Write((short)v);
                break;
            case BinDataType.Float32:
            default:
                foreach (var v in samples) _writer.Write((float)v);
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _writer?.Dispose();
        _disposed = true;
    }
}