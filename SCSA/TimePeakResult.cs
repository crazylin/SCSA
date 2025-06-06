using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA
{
    public class TimePeakResult
    {
        public double AveragePeak { set; get; }

        public double EffectivePeak { set; get; }
        public double PeakToPeak { set; get; }

        public double MaxPeak { set; get; }
        public double MinPeak { set; get; }

        public static TimePeakResult operator +(TimePeakResult f, TimePeakResult s)
        {
            f.AveragePeak += s.AveragePeak;
            f.EffectivePeak += s.EffectivePeak;
            f.PeakToPeak += s.PeakToPeak;
            f.MaxPeak += s.MaxPeak;
            f.MinPeak += s.MinPeak;
            return f;
        }
        public static TimePeakResult operator /(TimePeakResult f, double factor)
        {
            f.AveragePeak /= factor;
            f.EffectivePeak /= factor;
            f.PeakToPeak /= factor;
            f.MaxPeak /= factor;
            f.MinPeak /= factor;
            return f;
        }
    }
}
