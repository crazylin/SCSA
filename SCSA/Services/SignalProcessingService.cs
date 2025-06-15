using System;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using OxyPlot;
using SCSA.Models;
using SCSA.Services.Filter;

namespace SCSA.Services;

/// <summary>
///     提供常用的信号处理静态方法。
/// </summary>
public static class SignalProcessingService
{
    /// <summary>
    ///     计算 FFT 幅值谱，返回一半（正频段）数据。
    /// </summary>
    public static double[] ComputeFft(double[] samples)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<double>();

        var complex = samples.Select(d => new Complex(d, 0)).ToArray();
        Fourier.Forward(complex, FourierOptions.Matlab);
        var size = complex.Length / 2;
        return complex.Take(size).Select(c => c.Magnitude / size).ToArray();
    }

    /// <summary>
    ///     对输入信号进行频域滤波，并返回时域结果。
    /// </summary>
    public static double[] FilterSamples(FilterType type, double[] samples, double sampleRate,
        double lowPass = 0, double highPass = 0, double bandPassFirst = 0, double bandPassSecond = 0,
        double bandStopFirst = 0, double bandStopSecond = 0)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<double>();

        var freqData = samples.Select(d => new Complex(d, 0)).ToArray();
        Fourier.Forward(freqData, FourierOptions.Matlab);
        freqData = ApplyFilter(freqData, type, sampleRate, lowPass, highPass, bandPassFirst, bandPassSecond,
            bandStopFirst, bandStopSecond);
        Fourier.Inverse(freqData, FourierOptions.Matlab);
        return freqData.Select(c => c.Real).ToArray();
    }

    /// <summary>
    ///     根据筛选条件对频域数据进行滤波。
    /// </summary>
    public static Complex[] ApplyFilter(Complex[] data, FilterType filterType, double sampleRate,
        double lowPass, double highPass, double bandPassFirst, double bandPassSecond, double bandStopFirst = 0,
        double bandStopSecond = 0)
    {
        if (data == null) return Array.Empty<Complex>();

        var outData = data;
        switch (filterType)
        {
            case FilterType.LowPass:
                OnlineFilter.Filter(filterType, data, out outData, sampleRate, lowPass);
                break;
            case FilterType.HighPass:
                OnlineFilter.Filter(filterType, data, out outData, sampleRate, highPass);
                break;
            case FilterType.BandPass:
                OnlineFilter.Filter(filterType, data, out outData, sampleRate, bandPassFirst, bandPassSecond);
                break;
            case FilterType.BandStop:
                OnlineFilter.Filter(filterType, data, out outData, sampleRate, bandStopFirst, bandStopSecond);
                break;
        }

        return outData;
    }

    public static TimePeakResult CalculateTimePeak(double[] samples)
    {
        if (samples == null || samples.Length == 0)
            return new TimePeakResult();

        var max = samples.Max();
        var min = samples.Min();
        var sum = samples.Sum();
        var sumSquares = samples.Select(x => x * x).Sum();

        return new TimePeakResult
        {
            MaxPeak = max,
            MinPeak = min,
            PeakToPeak = max - min,
            AveragePeak = sum / samples.Length,
            EffectivePeak = Math.Sqrt(sumSquares / samples.Length)
        };
    }

    public static FreqPeakResult CalculateFreqPeak(DataPoint[] data)
    {
        if (data == null || data.Length == 0)
            return new FreqPeakResult();
        var dp = data.OrderByDescending(d => d.Y).First();
        return new FreqPeakResult { Position = dp.X, Peak = dp.Y };
    }
}