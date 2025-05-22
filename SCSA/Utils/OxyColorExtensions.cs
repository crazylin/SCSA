using Avalonia.Media;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Utils
{
    public static class OxyColorExtensions
    {
        // 扩展方法：OxyColor 转 Avalonia HslColor
        public static HslColor ToAvaloniaHsl(this OxyColor color)
        {
            var avaloniaColor = Color.FromArgb(color.A, color.R, color.G, color.B);
            return new HslColor(avaloniaColor);
        }

        // 扩展方法：Avalonia HslColor 转 OxyColor
        public static OxyColor ToOxyColor(this HslColor hsl)
        {
            return hsl.ToRgb().ToOxyColor();
        }

        // 调整色相（使用 Avalonia 原生实现）
        public static OxyColor ChangeHue(this OxyColor color, double delta)
        {
            var hsl = color.ToAvaloniaHsl();
            var newHue = (hsl.H + delta) % 360;
            return new HslColor(hsl.A, newHue, hsl.S, hsl.L).ToOxyColor();
        }

        // 调整亮度（使用 Avalonia 原生实现）
        public static OxyColor ChangeLightness(this OxyColor color, double factor)
        {
            var hsl = color.ToAvaloniaHsl();
            var newLightness = (hsl.L * factor).Clamp(0, 1);
            return new HslColor(hsl.A, hsl.H, hsl.S, newLightness).ToOxyColor();
        }

        // 调整饱和度（使用 Avalonia 原生实现）
        public static OxyColor ChangeSaturation(this OxyColor color, double factor)
        {
            var hsl = color.ToAvaloniaHsl();
            var newSaturation = (hsl.S * factor).Clamp(0, 1);
            return new HslColor(hsl.A, hsl.H, newSaturation, hsl.L).ToOxyColor();
        }

        // 安全数值范围限制（通用方法）
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (min.CompareTo(max) > 0) throw new ArgumentException("最小值不能大于最大值");
            return val.CompareTo(min) < 0 ? min : val.CompareTo(max) > 0 ? max : val;
        }

        // IBrush 转 OxyColor
        public static OxyColor ToOxyColor(this IBrush brush)
        {
            return brush switch
            {
                SolidColorBrush solid => solid.Color.ToOxyColor(),
                _ => OxyColors.Transparent
            };
        }

        // Avalonia.Color 转 OxyColor
        public static OxyColor ToOxyColor(this Color color)
        {
            return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static OxyColor WithAlpha(this OxyColor color, double alpha)
        {
            byte a = (byte)(255 * alpha.Clamp(0, 1));
            return OxyColor.FromArgb(a, color.R, color.G, color.B);
        }
    }
}
