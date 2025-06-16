using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SCSA.UFF;

/// <summary>
///     简化版 UFF 数据集 58 写入器。支持 ASCII 及二进制 58b（双精度偶采样）。
/// </summary>
public sealed class UFFStreamWriter : IDisposable
{
    private readonly UFFWriteFormat _format;
    private readonly List<double> _buffer = new(); // Binary 模式缓冲
    private readonly string _filePath;
    private bool _disposed;
    private readonly bool _append;
    private readonly bool _useFloat32;
    private readonly string _ordinateUnit;

    /// <summary>
    /// 采样率（Hz）。
    /// </summary>
    public double SampleRate { get; }

    public UFFStreamWriter(string filePath, UFFWriteFormat format, double sampleRate, bool append = false, bool useFloat32 = false, string ordinateUnit = "NONE")
    {
        _format = format;
        _filePath = filePath;
        SampleRate = sampleRate;
        _append = append;
        _useFloat32 = useFloat32;
        _ordinateUnit = ordinateUnit;
        // Binary 模式延迟写入，避免回写字节数。
    }

    #region 写入接口
    public void WriteSamples(ReadOnlySpan<double> samples)
    {
        EnsureNotDisposed();
        _buffer.AddRange(samples.ToArray());
    }
    #endregion

    #region Header 构造
    private void WriteCommonHeader(StreamWriter writer, long numBytes, bool isBinary, int sampleCount)
    {
        // Record 1: Start flag
        writer.WriteLine(FmtI(-1, 6));

        if (isBinary)
        {
            // Record 2: ID line for 58b (I6,A1,I6,I6,I12,I12,I6,I6,I12,I12) total 80 char
            var idLine = new StringBuilder();
            idLine.Append(FmtI(58, 6));
            idLine.Append('b');
            idLine.Append(FmtI(1, 6)); // Little-endian
            idLine.Append(FmtI(2, 6)); // IEEE 754
            idLine.Append(FmtI(11, 12)); // ASCII header lines count
            idLine.Append(FmtI(numBytes, 12)); // bytes following header
            idLine.Append(FmtI(0, 6));
            idLine.Append(FmtI(0, 6));
            idLine.Append(FmtI(0, 12));
            idLine.Append(FmtI(0, 12));
            writer.WriteLine(idLine.ToString().PadRight(80));
        }
        else
        {
            writer.WriteLine(FmtI(58, 6).PadRight(80));
        }

        // Records 3-7: 五条 ID line（用 NONE/日期等填充）
        writer.WriteLine(Pad80("Time Response"));
        writer.WriteLine(Pad80("NONE"));
        writer.WriteLine(Pad80(DateTime.Now.ToString("dd-MMM-yy HH:mm:ss", CultureInfo.InvariantCulture)));
        writer.WriteLine(Pad80("NONE"));
        writer.WriteLine(Pad80("NONE"));

        // Record 8 (line 6): DOF identification   Format 2(I5,I10),2(1X,10A1,I10,I4)
        var dof = new StringBuilder();
        dof.Append(FmtI(1, 5)); // Function Type 1 Time Response
        dof.Append(FmtI(0, 10)); // Function ID
        dof.Append(FmtI(0, 5)); // Version number
        dof.Append(FmtI(0, 10)); // Load case
        dof.Append(' ');
        dof.Append(FmtA("NONE", 10));
        dof.Append(FmtI(1, 10)); // Node
        dof.Append(FmtI(1, 4));  // Dir +X
        dof.Append(' ');
        dof.Append(FmtA("NONE", 10));
        dof.Append(FmtI(0, 10));
        dof.Append(FmtI(0, 4));
        writer.WriteLine(dof.ToString().PadRight(80));

        // Record 9 (line7): Data form Format(3I10,3E13.5)
        var dt = 1.0 / SampleRate;
        int ordType = _useFloat32 ? 2 : 4;
        var dataForm = string.Create(CultureInfo.InvariantCulture, $"{FmtI(ordType,10)}{FmtI(sampleCount,10)}{FmtI(1,10)}{FmtE(0,13,5)}{FmtE(dt,13,5)}{FmtE(0,13,5)}");
        writer.WriteLine(dataForm.PadRight(80));

        // Record 10 (line8): Abscissa characteristics  Format(I10,3I5,2(1X,20A1))
        writer.WriteLine(FormCharLine(17)); // time
        // Record 11 (line9): Ordinate characteristics，填写单位
        writer.WriteLine(FormCharLine(12, _ordinateUnit));
        // Record 12 (line10): Ordinate denom char (none) all zero
        writer.WriteLine(FormCharLine(0));
        // Record 13 (line11): Z-axis char (none)
        writer.WriteLine(FormCharLine(0));
    }

    private static string FormCharLine(int specType, string unit = "NONE")
    {
        var sb = new StringBuilder();
        sb.Append(FmtI(specType, 10));
        sb.Append(FmtI(0, 5));
        sb.Append(FmtI(0, 5));
        sb.Append(FmtI(0, 5));
        sb.Append(' ');
        sb.Append(FmtA("NONE", 20));
        sb.Append(' ');
        sb.Append(FmtA(unit, 20));
        return sb.ToString().PadRight(80);
    }
    #endregion

    #region Dispose & Binary flush
    public void Dispose()
    {
        if (_disposed) return;

        if (_format == UFFWriteFormat.ASCII)
        {
            WriteAsciiFile();
        }
        else
        {
            WriteBinaryFile();
        }

        _disposed = true;
    }

    private void WriteBinaryFile()
    {
        var numBytes = _buffer.Count * (_useFloat32 ? sizeof(float) : sizeof(double));
        using var fs = new FileStream(_filePath, _append ? FileMode.Append : FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(fs, Encoding.ASCII) { NewLine = "\n", AutoFlush = true };

        WriteCommonHeader(writer, numBytes, isBinary: true, sampleCount: _buffer.Count);
        writer.Flush();

        // 直接写入二进制数据
        using var bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);
        if (_useFloat32)
        {
            foreach (var v in _buffer) bw.Write((float)v);
        }
        else
        {
            foreach (var v in _buffer) bw.Write(v);
        }
        bw.Flush();

        // 结束 -1 行
        var endLine = ("\n" + FmtI(-1, 6) + "\n");
        var bytes = Encoding.ASCII.GetBytes(endLine);
        fs.Write(bytes, 0, bytes.Length);
        fs.Flush();
    }

    private void WriteAsciiFile()
    {
        using var writer = new StreamWriter(_filePath, _append, Encoding.ASCII) { NewLine = "\n" };

        WriteCommonHeader(writer, numBytes: 0, isBinary: false, sampleCount: _buffer.Count);
        writer.Flush();

        // 数据行：Case5 -> 4E20.12
        int idx = 0;
        while (idx < _buffer.Count)
        {
            var chunk = _buffer.Skip(idx).Take(4).ToArray();
            idx += chunk.Length;
            var line = string.Join("", chunk.Select(v => FmtE(v, 20, 12)));
            writer.WriteLine(line);
        }

        writer.WriteLine(FmtI(-1, 6));
    }
    #endregion

    #region 格式化辅助
    private static string FmtI(long value, int width) => value.ToString().PadLeft(width);

    private static string FmtA(string str, int width)
    {
        if (str.Length > width) str = str[..width];
        return str.PadRight(width);
    }

    private static string FmtE(double v, int width, int prec)
    {
        var s = v.ToString($"E{prec}", CultureInfo.InvariantCulture).ToUpper();
        // Remove leading zeros in exponent to fit FORTRAN style (e.g., E+00 -> E+0)
        s = System.Text.RegularExpressions.Regex.Replace(s, @"E([+-])0+", "E$1");
        return s.PadLeft(width);
    }

    private static string Pad80(string s) => (s.Length > 80 ? s[..80] : s).PadRight(80);
    #endregion

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UFFStreamWriter));
    }
} 