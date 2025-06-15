using SCSA.UFF;

namespace SCSA.Services.Recording.Writers;

/// <summary>
///     基于 UFFStreamWriter 的包装，当前仅支持单通道实数写入。
/// </summary>
internal sealed class UffChannelWriter : IChannelWriter
{
    private readonly UFFStreamWriter _writer;

    public UffChannelWriter(UFFStreamWriter writer)
    {
        _writer = writer;
    }

    public void Write(double[] samples)
    {
        // 同步调用异步方法，确保兼容当前 RecorderService 同步流程
        _writer.WriteDataAsync(samples).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}