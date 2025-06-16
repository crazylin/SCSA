using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SCSA.Plot;

/// <summary>
/// 将枚举值与 ConverterParameter 比较，返回 bool；反向将 true 转回参数枚举，false 返回 Binding.DoNothing。
/// 用于 RadioButton 与枚举互绑。
/// </summary>
public class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (b && parameter != null)
            {
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, parameter.ToString()!);
            }
            else if (!b)
            {
                // 当取消选中时返回枚举默认值（第一个定义的值，一般是 None）
                if (targetType.IsEnum)
                {
                    var values = Enum.GetValues(targetType);
                    if (values.Length > 0)
                        return values.GetValue(0);
                }
            }
        }

        return Avalonia.Data.BindingOperations.DoNothing;
    }
} 