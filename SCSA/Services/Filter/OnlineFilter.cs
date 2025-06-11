using System;
using System.Numerics;
using SCSA.Models;

namespace SCSA.Services.Filter;

public class OnlineFilter
{
    public static void Filter(FilterType type, Complex[] inData, out Complex[] outData, double sampleRate,
        double firstPass = 0,
        double secondPass = 0,
        double bandStopFirst = 0,
        double bandStopSecond = 0)
    {
        var attenuation = Math.Pow(10.0, 140 / 20.0); //100000;//
        switch (type)
        {
            case FilterType.LowPass:
                LowPass(inData, firstPass, sampleRate, out outData, attenuation);
                return;
            case FilterType.HighPass:
                HighPass(inData, firstPass, sampleRate, out outData, attenuation);
                return;
            case FilterType.BandPass:
                BandPass(inData, firstPass, secondPass, sampleRate, out outData, attenuation);
                return;
            case FilterType.BandStop:
                BandStop(inData, sampleRate, bandStopFirst, bandStopSecond, out outData, attenuation);
                return;
        }

        outData = inData;
    }

    public static void LowPass(Complex[] inData, double lowPass, double sampleRate, out Complex[] outData,
        double attenuation)
    {
        var cutoffIndex = inData.Length * lowPass / sampleRate;
        var halfSize = inData.Length / 2;

        outData = new Complex[inData.Length];
        var k = inData.Length - 1;
        for (var i = 0; i < halfSize; ++i)
        {
            if (i > cutoffIndex)
            {
                outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
                outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
            }
            else
            {
                outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
                outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
            }

            --k;
        }
    }

    public static void HighPass(Complex[] inData, double hightPass, double sampleRate, out Complex[] outData,
        double attenuation)
    {
        var cutoffIndex = inData.Length * hightPass / sampleRate;
        var halfSize = inData.Length / 2;

        outData = new Complex[inData.Length];
        var k = inData.Length - 1;
        for (var i = 0; i < halfSize; ++i)
        {
            if (i < cutoffIndex)
            {
                outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
                outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
            }
            else
            {
                outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
                outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
            }

            --k;
        }
    }

    public static void BandPass(Complex[] inData, double firstPass, double secondPass, double sampleRate,
        out Complex[] outData,
        double attenuation)
    {
        var lowCutoffIdx = inData.Length * firstPass / sampleRate;
        var highCutoffIdx = inData.Length * secondPass / sampleRate;
        var halfSize = inData.Length / 2;

        outData = new Complex[inData.Length];
        var k = inData.Length - 1;
        for (var i = 0; i < halfSize; ++i)
        {
            if (i < lowCutoffIdx || i > highCutoffIdx)
            {
                outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
                outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
            }
            else
            {
                outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
                outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
            }

            --k;
        }
    }

    public static void BandStop(Complex[] inData, double firstPass, double secondPass, double sampleRate,
        out Complex[] outData,
        double attenuation)
    {
        var lowCutoffIdx = inData.Length * firstPass / sampleRate;
        var highCutoffIdx = inData.Length * secondPass / sampleRate;
        var halfSize = inData.Length / 2;

        outData = new Complex[inData.Length];
        var k = inData.Length - 1;
        for (var i = 0; i < halfSize; ++i)
        {
            if (i < lowCutoffIdx || i > highCutoffIdx)
            {
                outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
                outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
            }
            else
            {
                outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
                outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
            }

            --k;
        }
    }
}