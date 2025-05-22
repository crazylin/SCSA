using Avalonia.Data.Converters;
using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using SCSA.Models;

namespace SCSA.Converters
{


    public class RadioButtonSelectedValueConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // values[0]: 所有选项 (IEnumerable<EnumOption>)
            // values[1]: 当前选中的值 (object)

            if (values[0] is IEnumerable<EnumOption> options &&
                values[1] != null &&
                values[1].GetType() != typeof(UnsetValueType))
            {
                // 找到与 SelectedValue 匹配的 EnumOption
                return options.FirstOrDefault(o => Equals(o.RealValue, values[1]));
            }
            return null;
        }

        public object[] ConvertBack(object? value)
        {
            if (value is EnumOption selectedOption)
            {
                // 返回 [null, RealValue]，因为只需要更新 SelectedValue
                return new object[]
                {
                    null, // Options 不需要更新
                    selectedOption.RealValue // 返回选中的实际值
                };
            }
            return new object[] { null, null }; // 无选中项
        }
    }

}
