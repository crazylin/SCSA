using Avalonia.Data.Converters;
using Avalonia.Utilities;
using System;
using System.Globalization;

namespace SCSA.Converters
{
    public class GreaterThanOrEqualZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue >= 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 