using System;

namespace SCSA.Services.Recording.Writers;

/// <summary>
///     针对单通道数据的写入器抽象。
/// </summary>
public interface IChannelWriter : IDisposable
{
    void Write(double[] samples);
}