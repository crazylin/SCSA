using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace SCSA.Converters;

public class BoolToStartStopTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string baseText = parameter as string ?? "监听";
        if (value is bool b)
            return b ? $"停止{baseText}" : $"开始{baseText}";
        return $"开始{baseText}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 