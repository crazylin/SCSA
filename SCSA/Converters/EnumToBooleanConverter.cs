using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SCSA.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return enumValue.Equals(targetValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return null;

        var useValue = (bool)value;
        var targetValue = parameter.ToString();
        if (useValue)
            return Enum.Parse(targetType, targetValue);

        return null;
    }
}