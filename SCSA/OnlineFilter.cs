
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SCSA.Models;

namespace SCSA
{
    public class OnlineFilter
    {
        public static void Filter(FilterType type, Complex[] inData, out Complex[] outData, double sampleRate,
            double firstPass = 0,
            double secondPass = 0,
            double bandStopFirst = 0,
            double bandStopSecond = 0)
        {

            double attenuation = Math.Pow(10.0, 140 / 20.0);//100000;//
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
            double cutoffIndex = inData.Length * lowPass / sampleRate;
            int halfSize = inData.Length / 2;

            outData = new Complex[inData.Length];
            int k = inData.Length - 1;
            for (int i = 0; i < halfSize; ++i)
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
            double cutoffIndex = inData.Length * hightPass / (double)sampleRate;
            int halfSize = inData.Length / 2;

            outData = new Complex[inData.Length];
            int k = inData.Length - 1;
            for (int i = 0; i < halfSize; ++i)
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
            double lowCutoffIdx = inData.Length * firstPass / sampleRate;
            double highCutoffIdx = inData.Length * secondPass / sampleRate;
            int halfSize = inData.Length / 2;

            outData = new Complex[inData.Length];
            int k = inData.Length - 1;
            for (int i = 0; i < halfSize; ++i)
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
            double lowCutoffIdx = inData.Length * firstPass / sampleRate;
            double highCutoffIdx = inData.Length * secondPass / sampleRate;
            int halfSize = inData.Length / 2;

            outData = new Complex[inData.Length];
            int k = inData.Length - 1;
            for (int i = 0; i < halfSize; ++i)
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

        //public static void MultiBandPass(Complex[] inData, double sampleRate, List<BandFilterSetting> models, bool isBandStop,
        //    out Complex[] outData)
        //{
        //    if (models == null || models.Count == 0)
        //    {
        //        outData = inData;
        //        return;
        //    }

        //    int halfSize = inData.Length / 2;
        //    int k = inData.Length - 1;
        //    outData = new Complex[inData.Length];
        //    int prevtail = 0;
        //    for (int index = 0; index < models.Count; ++index)
        //    {
        //        BandFilterSetting m = models.ElementAt(index);
        //        //if (!m.isEnabled)  continue;

        //        double lowFrequency = m.FirstPass;
        //        double highFrequency = m.SecondPass;
        //        double attenuation = Math.Pow(10, m.Attenuation / 20.0);
        //        double lowCutoffIdx = inData.Length * lowFrequency / sampleRate;
        //        double highCutoffIdx = inData.Length * highFrequency / sampleRate;

        //        int endTail = (int)highCutoffIdx;
        //        if (endTail >= halfSize || index == models.Count - 1)
        //        {
        //            endTail = halfSize;
        //        }

        //        int startTail = prevtail;
        //        if (index == 0)
        //        {
        //            startTail = 0;
        //        }

        //        for (int i = startTail; i < endTail; ++i)
        //        {
        //            if (i >= lowCutoffIdx && i <= highCutoffIdx) // inside the filter
        //            {
        //                if (isBandStop) // eliminate the signal
        //                {
        //                    outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
        //                    outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
        //                }
        //                else // let the signal pass
        //                {
        //                    outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
        //                    outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
        //                }
        //            }
        //            else // outside the filter
        //            {
        //                if (isBandStop) // let the signal pass
        //                {
        //                    outData[i] = new Complex(inData[i].Real, inData[i].Imaginary);
        //                    outData[k] = new Complex(inData[k].Real, inData[k].Imaginary);
        //                }
        //                else
        //                {
        //                    outData[i] = new Complex(inData[i].Real / attenuation, inData[i].Imaginary / attenuation);
        //                    outData[k] = new Complex(inData[k].Real / attenuation, inData[k].Imaginary / attenuation);
        //                }
        //            }

        //            --k;
        //        }

        //        prevtail = endTail;
        //    }
        //}
    }
}
