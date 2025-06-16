using SCSA.UFF;

namespace SCSA.Services.Recording.Writers;

/// <summary>
///     UFF 通道写入器包装类，可选是否在 Dispose 时关闭底层 <see cref="UFFStreamWriter"/>。
///     仅支持单通道实数数据写入。
/// </summary>
internal sealed class UffChannelWriter : IChannelWriter
{
    private readonly UFFStreamWriter _streamWriter;
    private readonly bool _leaveOpen;
    private bool _disposed;

    public UffChannelWriter(UFFStreamWriter streamWriter, bool leaveOpen = false)
    {
        _streamWriter = streamWriter;
        _leaveOpen = leaveOpen;
    }

    public void Write(double[] samples)
    {
        if (_disposed) return;
        _streamWriter.WriteSamples(samples);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (!_leaveOpen) _streamWriter.Dispose();
        _disposed = true;
    }
} 