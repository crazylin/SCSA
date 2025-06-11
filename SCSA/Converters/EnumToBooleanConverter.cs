using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SCSA.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string enumValue = value.ToString();
        string targetValue = parameter.ToString();

        return enumValue.Equals(targetValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return null;

        bool useValue = (bool)value;
        string targetValue = parameter.ToString();
        if (useValue)
            return Enum.Parse(targetType, targetValue);

        return null;
    }
}