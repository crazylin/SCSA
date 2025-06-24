using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SCSA.Utils;

namespace SCSA.Converters
{
    public class PhysicalUnitToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is PhysicalUnit unit)
            {
                return UnitConverter.GetUnitString(unit);
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
} 