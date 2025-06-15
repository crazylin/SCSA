using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SCSA.ViewModels;

namespace SCSA.Converters;

public class EnumToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
            return enumValue switch
            {
                StorageType.Length => "按数据长度",
                StorageType.Time => "按存储时间",
                FileFormatType.Binary => "二进制格式 (BIN)",
                FileFormatType.UFF => "UFF",
                FileFormatType.WAV => "WAV",
                UFFFormatType.ASCII => "ASCII格式",
                UFFFormatType.Binary => "二进制格式",
                _ => enumValue.ToString()
            };
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}