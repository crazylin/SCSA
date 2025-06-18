using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SCSA.Models;

// ParameterItem.cs
public abstract class DeviceParameter : ReactiveObject
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Address { get; init; }
    public int DataLength { get; init; }
    public Type ValueType { get; set; }

    public string Category { get; set; } = "默认";

    public abstract object Value { get; set; }
}

public class ParameterCategory
{
    public ParameterCategory(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public ObservableCollection<DeviceParameter> Parameters { get; } = new();
}

public class StringParameter : DeviceParameter
{
    [Reactive] public override object Value { get; set; }
}

public class BoolParameter : DeviceParameter
{
    [Reactive] public override object Value { get; set; }
}

public class NumberParameter : DeviceParameter
{
    public double MinValue { get; set; } = 0;
    public double MaxValue { get; set; } = 100;

    [Reactive] public override object Value { get; set; }
}

public class FloatNumberParameter : DeviceParameter
{
    public float MinValue { get; set; } = 0;
    public float MaxValue { get; set; } = 100;

    [Reactive] public override object Value { get; set; }
}

public class IntegerNumberParameter : DeviceParameter
{
    public int MinValue { get; set; } = 0;
    public int MaxValue { get; set; } = 100;

    [Reactive] public override object Value { get; set; }
}

public class EnumParameter : DeviceParameter
{
    public List<EnumOption> Options { set; get; } = new();

    [Reactive] public override object Value { get; set; }
}

public record EnumOption(string DisplayName, object RealValue);

public class EnumCheckParameter : DeviceParameter
{
    private List<object> _selectedValues = new();
    public List<EnumOption> Options { get; set; } = new();

    public List<object> SelectedValues
    {
        get => _selectedValues;
        set
        {
            if (_selectedValues != value)
            {
                _selectedValues = value;

                this.RaisePropertyChanged();
                Value = _selectedValues; // 同步到 Value 属性
            }
        }
    }

    public override object Value
    {
        get => _selectedValues;
        set
        {
            if (value is IEnumerable<object> enumerable)
                _selectedValues = enumerable.ToList();
            else if (value != null)
                _selectedValues = new List<object> { value };
            else
                _selectedValues = new List<object>();

            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SelectedValues));
        }
    }
}

public class EnumRadioParameter : DeviceParameter
{
    public List<EnumOption> Options { set; get; } = new();

    [Reactive] public override object Value { get; set; }
}
//public class EnumRadioParameter : DeviceParameter
//{
//    public List<EnumOption> Options { get; set; } = new();
//    public override object Value { get; set; }
//}