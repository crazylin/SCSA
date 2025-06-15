using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SCSA.UFF.Models;

namespace SCSA.UFF;

public class UFFStreamWriter : IDisposable
{
    private readonly BinaryWriter _binaryWriter;
    private readonly string _filePath;
    private readonly UFFWriteFormat _format;
    private readonly StreamWriter _writer;
    private long _currentDataPoints;
    private bool _isDisposed;
    private bool _isInitialized;
    private IProgress<int> _progress;
    private long _totalDataPoints;

    public UFFStreamWriter(string filePath, UFFWriteFormat format)
    {
        _filePath = filePath;
        _format = format;

        if (format == UFFWriteFormat.ASCII)
            _writer = new StreamWriter(filePath, false, Encoding.ASCII);
        else
            _binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.Write));
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_format == UFFWriteFormat.ASCII)
            _writer?.Dispose();
        else
            _binaryWriter?.Dispose();

        _isDisposed = true;
    }

    public void SetProgress(IProgress<int> progress)
    {
        _progress = progress;
    }

    public async Task InitializeStreamAsync(
        double sampleRate,
        string channelName,
        string yUnit,
        string description,
        long targetDataLength,
        bool isIQSignal = false,
        string title = "SCSA Signal Recording")
    {
        if (_isInitialized)
            return;

        _totalDataPoints = targetDataLength;
        _currentDataPoints = 0;

        // 写入数据集开始标记
        if (_format == UFFWriteFormat.ASCII)
        {
            await _writer.WriteAsync("    -1\n");
            await _writer.WriteAsync("    58\n");
        }
        else
        {
            _binaryWriter.Write(-1);
            _binaryWriter.Write(58);
        }

        // 写入数据集头部信息
        var header = new StringBuilder();

        // 标识行
        header.AppendLine($"{title,-80}");
        header.AppendLine($"{description,-80}");
        header.AppendLine($"{DateTime.Now:dd-MMM-yy HH:mm:ss,-80}");
        header.AppendLine($"{channelName,-80}");
        header.AppendLine("NONE".PadRight(80));

        // 函数类型和ID
        header.AppendLine($"    {1,6}    {1,6}    {1,6}    {0,6}"); // 函数类型=1(时域响应), ID=1, 版本=1, 工况=0

        // 节点和方向信息
        header.AppendLine($"{channelName,-80}");
        header.AppendLine($"    {1,6}    {1,6}    {1,6}    {1,6}"); // 响应节点=1, 响应方向=1, 参考节点=1, 参考方向=1

        // 数据格式信息
        var ordDataType = isIQSignal ? 6 : 4; // 6=复数双精度, 4=实数双精度
        header.AppendLine(
            $"    {ordDataType,6}    {targetDataLength,6}    {1,6}    {0.0,13:E4}    {1.0 / sampleRate,13:E4}    {0.0,13:E4}");

        // 坐标轴信息
        header.AppendLine($"    {18,6}    {0,6}"); // X轴类型=18(频率), Y轴类型=0(未知)
        header.AppendLine("Time".PadRight(20) + "s".PadRight(20));
        header.AppendLine("Amplitude".PadRight(20) + $"{yUnit}".PadRight(20));

        if (_format == UFFWriteFormat.ASCII)
            await _writer.WriteAsync(header.ToString());
        else
            // 二进制格式：写入头部信息
            _binaryWriter.Write(Encoding.ASCII.GetBytes(header.ToString()));

        _isInitialized = true;
    }

    public async Task WriteDataAsync(double[] data)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Stream not initialized");

        if (_format == UFFWriteFormat.ASCII)
            // 每行写入4个值
            for (var i = 0; i < data.Length; i += 4)
            {
                var remaining = Math.Min(4, data.Length - i);
                var line = new StringBuilder();

                for (var j = 0; j < remaining; j++) line.Append($"{data[i + j],20:E12}");

                await _writer.WriteAsync(line + "\n");
            }
        else
            // 二进制格式：直接写入双精度数据
            foreach (var value in data)
                _binaryWriter.Write(value);

        _currentDataPoints += data.Length;
        _progress?.Report((int)((double)_currentDataPoints / _totalDataPoints * 100));
    }

    public async Task WriteIQDataAsync(double[] iData, double[] qData)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Stream not initialized");

        if (iData.Length != qData.Length)
            throw new ArgumentException("I and Q data must have the same length");

        if (_format == UFFWriteFormat.ASCII)
            // 每行写入2对IQ值
            for (var i = 0; i < iData.Length; i += 2)
            {
                var remaining = Math.Min(2, iData.Length - i);
                var line = new StringBuilder();

                for (var j = 0; j < remaining; j++) line.Append($"{iData[i + j],20:E12}{qData[i + j],20:E12}");

                await _writer.WriteAsync(line + "\n");
            }
        else
            // 二进制格式：交替写入I和Q数据
            for (var i = 0; i < iData.Length; i++)
            {
                _binaryWriter.Write(iData[i]);
                _binaryWriter.Write(qData[i]);
            }

        _currentDataPoints += iData.Length;
        _progress?.Report((int)((double)_currentDataPoints / _totalDataPoints * 100));
    }

    public async Task FinalizeAsync()
    {
        if (!_isInitialized)
            return;

        // 写入数据集结束标记
        if (_format == UFFWriteFormat.ASCII)
        {
            await _writer.WriteAsync("    -1\n");
            await _writer.FlushAsync();
        }
        else
        {
            _binaryWriter.Write(-1);
            _binaryWriter.Flush();
        }
    }
}