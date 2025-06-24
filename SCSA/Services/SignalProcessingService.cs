using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using OxyPlot;
using OxyPlot.Series;
using SCSA.Models;
using SCSA.Services.Filter;
using SCSA.Utils;
using SCSA.ViewModels;

namespace SCSA.Services;

/// <summary>
/// A simple record to hold data for an octave band, avoiding direct dependency on UI types.
/// </summary>
public record OctaveBandData(double Value, double CategoryIndex);

/// <summary>
///     提供常用的信号处理静态方法。
/// </summary>
public static class SignalProcessingService
{
    private static readonly double[] OneThirdOctaveNominalFrequencies = {
        20, 25, 31.5, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000,
        1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000
    };

    public static List<OctaveBandData> ComputeOneThirdOctave(double[] powerSpectrum, double sampleRate)
    {
        var N = (powerSpectrum.Length) * 2;
        if (N == 0) return new List<OctaveBandData>();

        var df = sampleRate / N;
        var result = new List<OctaveBandData>();

        foreach (var centerFreq in OneThirdOctaveNominalFrequencies)
        {
            if (centerFreq > sampleRate / 2) break;

            var fLower = centerFreq / Math.Pow(2, 1.0 / 6.0);
            var fUpper = centerFreq * Math.Pow(2, 1.0 / 6.0);

            var startIndex = (int)Math.Ceiling(fLower / df);
            var endIndex = (int)Math.Floor(fUpper / df);

            if (startIndex >= powerSpectrum.Length) continue;
            endIndex = Math.Min(endIndex, powerSpectrum.Length - 1);

            double sumOfSquares = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                sumOfSquares += powerSpectrum[i];
            }

            // The result is the RMS value of the band
            result.Add(new OctaveBandData(Math.Sqrt(sumOfSquares), (int)centerFreq));
        }

        return result;
    }

    /// <summary>
    ///     计算 FFT 幅值谱，返回一半（正频段）数据。
    /// </summary>
    public static double[] ComputeFft(double[] samples, double sampleRate, WindowFunction window, SpectrumType spectrumType)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<double>();

        var N = samples.Length;
        var complex = samples.Select(d => new Complex(d, 0)).ToArray();
        
        // Use NoScaling to get raw FFT output, then apply scaling manually
        Fourier.Forward(complex, FourierOptions.NoScaling);
        
        var size = N / 2;
        var spectrum = new double[size];
        
        // See https://www.sjsu.edu/people/burford.furman/docs/me120/FFT_tutorial_NI.pdf for scaling factors
        var windowScalingFactor = Window.GetWindow(window, N).Sum();

        for (int i = 0; i < size; i++)
        {
            // Amplitude scaling
            var amplitude = complex[i].Magnitude * 2 / windowScalingFactor;
            
            switch (spectrumType)
            {
                case SpectrumType.Amplitude:
                    spectrum[i] = amplitude;
                    break;
                case SpectrumType.Power:
                    spectrum[i] = Math.Pow(amplitude / Math.Sqrt(2), 2);
                    break;
                case SpectrumType.PowerSpectralDensity:
                    var enbw = Window.GetEnbw(window);
                    var df = sampleRate / N;
                    spectrum[i] = Math.Pow(amplitude / Math.Sqrt(2), 2) / (enbw * df);
                    break;
            }
        }
        return spectrum;
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