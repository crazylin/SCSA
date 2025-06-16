using System;
using System.IO;
using System.Text;

namespace SCSA.Services.Recording.Writers;

/// <summary>
///     将采样数据写入单声道 16-bit PCM WAV 文件。
/// </summary>
internal sealed class WavChannelWriter : IChannelWriter
{
    private readonly int _sampleRate;
    private readonly BinaryWriter _writer;
    private long _dataLength;
    private bool _disposed;

    public WavChannelWriter(BinaryWriter writer, int sampleRate)
    {
        _writer = writer;
        _sampleRate = sampleRate;
        WriteHeaderPlaceholder();
    }

    public void Write(double[] samples)
    {
        if (_disposed) return;
        foreach (var v in samples)
        {
            // 将 -1.0~1.0 范围映射到 16-bit 有符号整型
            var scaled = Math.Round(v * short.MaxValue);
            var s = (short)Math.Clamp(scaled, short.MinValue, short.MaxValue);
            _writer.Write(s);
            _dataLength += 2;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        // 回写尺寸信息
        var fileSize = 4 + 8 + 16 + 8 + _dataLength;
        _writer.Seek(4, SeekOrigin.Begin);
        _writer.Write((int)fileSize);
        _writer.Seek(40, SeekOrigin.Begin);
        _writer.Write((int)_dataLength);
        _writer.Dispose();
        _disposed = true;
    }

    private void WriteHeaderPlaceholder()
    {
        // RIFF header placeholder (44 bytes)
        _writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        _writer.Write(0); // placeholder for chunk size
        _writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        _writer.Write(Encoding.ASCII.GetBytes("fmt "));
        _writer.Write(16); // PCM header length
        _writer.Write((short)1); // PCM
        _writer.Write((short)1); // channels
        _writer.Write(_sampleRate);
        _writer.Write(_sampleRate * 2); // byteRate = sampleRate * channels * bits/8
        _writer.Write((short)2); // blockAlign = channels * bits/8
        _writer.Write((short)16); // bits per sample
        _writer.Write(Encoding.ASCII.GetBytes("data"));
        _writer.Write(0); // data chunk size placeholder
    }
}