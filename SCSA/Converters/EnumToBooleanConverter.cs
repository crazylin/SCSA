using Avalonia.Data.Converters;
using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace SCSA.Converters
{

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // 检查枚举值与参数是否匹配
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                // 返回对应的枚举值
                return Enum.Parse(targetType, parameter.ToString());
            }
            // 取消更新绑定源
            return AvaloniaProperty.UnsetValue; // 或返回 new BindingNotification(AvaloniaProperty.UnsetValue, BindingNotificationType.DoNothing);
        }
    }


}
