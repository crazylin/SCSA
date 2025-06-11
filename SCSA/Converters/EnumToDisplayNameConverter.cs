using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SCSA.Converters
{
    public class EnumToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue switch
                {
                    SCSA.ViewModels.StorageType.ByLength => "按数据长度",
                    SCSA.ViewModels.StorageType.ByTime => "按存储时间",
                    _ => enumValue.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 