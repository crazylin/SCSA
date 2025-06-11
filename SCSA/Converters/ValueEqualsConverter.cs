using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SCSA.Converters;

public class ValueEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? parameter : AvaloniaProperty.UnsetValue;
    }
}