using System.Globalization;
using Avalonia.Data.Converters;
using SCSA.Plot;

namespace SCSA.Plot;

public class InteractionModeToVisibilityConverter : IValueConverter
{
    // 新增反转显示参数（XAML中可配置）
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is InteractionMode mode)
        {
            var shouldShow = mode == InteractionMode.RangeSelect;
            if (Invert) shouldShow = !shouldShow;

            return shouldShow;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}