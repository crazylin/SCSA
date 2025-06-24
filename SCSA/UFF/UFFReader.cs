using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SCSA.Utils;

namespace SCSA.UFF;

public record UFFTimeSeries(double SampleRate, string Unit, double[] Samples);

/// <summary>
/// 读取本项目写出的 UFF (Dataset 58 / 58b) 文件，仅支持 real-even-spacing 单通道数据。
/// </summary>
public static class UFFReader
{
    public static List<UFFTimeSeries> Read(string filePath)
    {
        var result = new List<UFFTimeSeries>();
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var sr = new StreamReader(fs, Encoding.ASCII);
        while (!sr.EndOfStream)
        {
            try
            {
                // 寻找起始 -1 行
                var line = ReadLineSkipEmpty(sr);
                if (line == null) break;
                if (!line.TrimStart().StartsWith("-1")) continue;
                // 下一行 ID
                var idLine = sr.ReadLine();
                if (idLine == null) break;
                var idStr = idLine.Substring(0, 6).Trim();
                if (idStr != "58")
                {
                    // 若是58b 则带 b
                    if (!idStr.StartsWith("58"))
                    {
                        // 跳过未知数据集
                        SkipUntilEnd(sr);
                        continue;
                    }
                }
                bool isBinary = idLine.Length >= 7 && idLine[6] == 'b';
                if (isBinary)
                {
                    // Parse fields to get nAscii and nBytes
                    int nAscii = int.Parse(idLine.Substring(13, 12));
                    long nBytes = long.Parse(idLine.Substring(25, 12));
                    // 读取 header 剩余 nAscii-1 行 (我们已读1行)
                    var headerLines = new List<string>();
                    for (int i = 0; i < nAscii - 1; i++) headerLines.Add(sr.ReadLine());
                    ParseHeader(headerLines, out double srHz, out string unit, out int ordType);
                    // 读取二进制数据
                    var dataBytes = new byte[nBytes];
                    fs.Read(dataBytes, 0, (int)nBytes);
                    var count = (ordType == 2) ? nBytes / 4 : nBytes / 8;
                    var samples = new double[count];
                    if (ordType == 2)
                    {
                        for (int i = 0; i < count; i++)
                            samples[i] = BitConverter.ToSingle(dataBytes, i * 4);
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                            samples[i] = BitConverter.ToDouble(dataBytes, i * 8);
                    }
                    // 读取结束行 -1
                    // 跳过结束 -1 行
                    SkipUntilEnd(sr);
                    result.Add(new UFFTimeSeries(srHz, unit, samples));
                }
                else
                {
                    // ASCII: 读取接下来的 11-1=10 条 header
                    var headerLines = new List<string>();
                    for (int i = 0; i < 11 - 1; i++)
                    {
                        var lineToAdd = sr.ReadLine();
                        if (lineToAdd == null) break; // End of stream reached prematurely
                        headerLines.Add(lineToAdd);
                    }
                    ParseHeader(headerLines, out double srHz, out string unit, out int ordType);
                    if (srHz == 0) continue; // Invalid header, skip dataset

                    int numPts = int.Parse(headerLines[5].Substring(10, 10));
                    var samples = new double[numPts];
                    int idx = 0;
                    while (idx < numPts)
                    {
                        var dLine = sr.ReadLine();
                        if (dLine == null) break;
                        if (dLine.TrimStart().StartsWith("-1")) break; // safety
                        for (int pos = 0; pos + 20 <= dLine.Length && idx < numPts; pos += 20)
                        {
                            var chunk = dLine.Substring(pos, 20);
                            if (double.TryParse(chunk, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                                samples[idx++] = v;
                        }
                    }
                    // 读结束行
                    if (!sr.EndOfStream) sr.ReadLine();
                    result.Add(new UFFTimeSeries(srHz, unit, samples));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to parse a UFF dataset in '{filePath}'. Skipping to next dataset.", ex);
                SkipUntilEnd(sr); // Attempt to recover by finding the next dataset
            }
        }
        return result;
    }

    private static void ParseHeader(List<string> lines, out double sampleRate, out string unit, out int ordType)
    {
        sampleRate = 0;
        unit = "";
        ordType = 0;

        // A valid header must have at least 7 lines for what we need.
        if (lines.Count < 7)
        {
            return;
        }

        // lines[3] 是 DOF identification, lines[4] DataForm, lines[5] Abscissa char, lines[6] ordinate char
        var dataForm = lines[4];
        ordType = int.Parse(dataForm.Substring(0, 10));
        double inc = double.Parse(dataForm.Substring(33, 13), CultureInfo.InvariantCulture);
        sampleRate = 1.0 / inc;
        var ordinateLine = lines[6];
        unit = ordinateLine.Length >= 60 ? ordinateLine.Substring(41, 20).Trim() : "";
    }

    private static void SkipUntilEnd(StreamReader sr)
    {
        string line;
        while ((line = sr.ReadLine()) != null)
            if (line.TrimStart().StartsWith("-1")) break;
    }

    private static string ReadLineSkipEmpty(StreamReader sr)
    {
        string line;
        do
        {
            line = sr.ReadLine();
        } while (line != null && line.Length == 0);
        return line;
    }
} 