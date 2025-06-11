using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SCSA.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTestRunning && parameter is bool enableDataStorage)
            {
                return isTestRunning && !enableDataStorage;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 