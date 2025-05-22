using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace SCSA.ViewModels
{
    public partial class MainWindowViewModel(
        ConnectionViewModel connectionViewModel,
        RealTimeTestViewModel realTimeTestViewModel,
        FirmwareUpdateViewModel firmwareUpdateViewModel)
        : ViewModelBase
    {

        public FirmwareUpdateViewModel FirmwareUpdateViewModel { set; get; } = firmwareUpdateViewModel;
        public ConnectionViewModel ConnectionViewModel { set; get; } = connectionViewModel;
        public RealTimeTestViewModel RealTimeTestViewModel { set; get; } = realTimeTestViewModel;

        [ObservableProperty] private object _contentView;
        [ObservableProperty]
        private object _selectedItem;

        //public object SelectedItem
        //{
        //    set
        //    {
        //        if (Equals(value, _selectedItem)) return;
        //        _selectedItem = value;
        //        var v = (dynamic)_selectedItem;
        //        switch (int.Parse(v.Tag.ToString()))
        //        {
        //            case 0:
        //                ContentView = ConnectionViewModel;
        //                break;
        //            case 1:
        //                ContentView = RealTimeTestViewModel;
        //                break;
        //            case 2:
        //                ContentView = FirmwareUpdateViewModel;
        //                break;
        //        }
        //        OnPropertyChanged();
        //    }
        //    get => _selectedItem;
        //}

    }
}
