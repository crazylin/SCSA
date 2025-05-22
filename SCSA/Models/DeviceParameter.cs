using SCSA.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    // ParameterItem.cs
    public abstract  class DeviceParameter : ViewModelBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Address { get; init; }
        public int DataLength { get; init; }
        public abstract object Value { get; set; }
        public Type ValueType { get; set; }

        public string Category { get; set; } = "默认";
    }
    public class ParameterCategory
    {
        public string Name { get; }
        public ObservableCollection<DeviceParameter> Parameters { get; } = new();

        public ParameterCategory(string name) => Name = name;
    }

    public class StringParameter : DeviceParameter
    {
        private string _value;
        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, (string)value);
        }
    }

    public class BoolParameter : DeviceParameter
    {
        private bool _value;
        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, Convert.ToBoolean(value));
        }
    }

    public class NumberParameter : DeviceParameter
    {
        private double _value;
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 100;

        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, Convert.ToDouble(value));
        }
    }

    public class FloatNumberParameter : DeviceParameter
    {
        private float _value;
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = 100;

        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, Convert.ToSingle(value));
        }
    }
    public class IntegerNumberParameter : DeviceParameter
    {
        private int _value;
        public int MinValue { get; set; } = 0;
        public int MaxValue { get; set; } = 100;

        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, Convert.ToInt32(value));
        }
    }
    
    public class EnumParameter : DeviceParameter
    {
        public List<EnumOption> Options { set; get; } = new();
        private object _value;

        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    public record EnumOption(string DisplayName, object RealValue);

    public class EnumCheckParameter : DeviceParameter
    {
        public List<EnumOption> Options { get; set; } = new();

        private List<object> _selectedValues = new List<object>();
        public List<object> SelectedValues
        {
            get => _selectedValues;
            set
            {
                if (_selectedValues != value)
                {
                    _selectedValues = value;
                    OnPropertyChanged(nameof(SelectedValues));
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
                {
                    _selectedValues = enumerable.ToList();
                }
                else if (value != null)
                {
                    _selectedValues = new List<object> { value };
                }
                else
                {
                    _selectedValues = new List<object>();
                }
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(SelectedValues));
            }
        }
    }

    public class EnumRadioParameter:DeviceParameter
    {
        public List<EnumOption> Options { set; get; } = new();
        private object _value;

        public override object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
    //public class EnumRadioParameter : DeviceParameter
    //{
    //    public List<EnumOption> Options { get; set; } = new();
    //    public override object Value { get; set; }
    //}
}
