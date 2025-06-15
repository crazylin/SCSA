using System.Collections.Generic;
using ReactiveUI;

namespace SCSA.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private NavItem _selectedItem;

    public MainWindowViewModel(ConnectionViewModel connectionViewModel,
        RealTimeTestViewModel realTimeTestViewModel,
        FirmwareUpdateViewModel firmwareUpdateViewModel,
        SettingsViewModel settingsViewModel)
    {
        ConnectionViewModel = connectionViewModel;
        RealTimeTestViewModel = realTimeTestViewModel;
        FirmwareUpdateViewModel = firmwareUpdateViewModel;
        SettingsViewModel = settingsViewModel;

        NavItems = new List<NavItem>
        {
            new("设备管理", ConnectionViewModel, "ViewAll"),
            new("实时测试", RealTimeTestViewModel, "Play"),
            new("固件升级", FirmwareUpdateViewModel, "Sync")
        };

        SelectedItem = NavItems[0];


        FooterNavItems = new List<NavItem> { new("设置", SettingsViewModel, "Setting") };
    }

    public IReadOnlyList<NavItem> NavItems { get; }

    public NavItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            this.RaisePropertyChanged(nameof(CurrentPage));
        }
    }

    public IReadOnlyList<NavItem> FooterNavItems { get; set; }

    public object CurrentPage => SelectedItem?.Page;

    public ConnectionViewModel ConnectionViewModel { get; }
    public RealTimeTestViewModel RealTimeTestViewModel { get; }
    public FirmwareUpdateViewModel FirmwareUpdateViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public record NavItem(string Title, object Page, object Icon);
}