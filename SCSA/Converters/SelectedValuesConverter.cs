using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data.Converters;
using SCSA.Models;

namespace SCSA.Converters
{
    public class SelectedValuesConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // 检查 values[0] 是否是 IEnumerable<EnumOption>
            if (values[0] is IEnumerable<EnumOption> options && values[1] != null && values[1].GetType() != typeof(UnsetValueType))
            {
                // 处理 values[1]：可能是单一值或集合
                HashSet<object> selectedValues = new HashSet<object>();

                if (values[1] is IEnumerable enumerable && !(values[1] is string)) // 排除字符串
                {
                    // 如果是集合，遍历并添加到 HashSet
                    foreach (var item in enumerable)
                    {
                        selectedValues.Add(item);
                    }
                }
                else
                {
                    // 如果是单一值，直接添加到 HashSet
                    selectedValues.Add(values[1]);
                }


                // 返回匹配的 EnumOption 列表
                return options.Where(o => selectedValues.Contains(o.RealValue)).ToList();
            }
            return null;
        }

        public object[] ConvertBack(object value)
        {
            if (value is IEnumerable<EnumOption> selectedOptions)
            {
                return new object[]
                {
                    null, // Options 不需要更新
                    selectedOptions.Select(o => o.RealValue).ToList() // 返回 List<object>
                };
            }
            return new object[2];
        }
    }


}
