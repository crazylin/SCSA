using System.Collections.Generic;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SCSA.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<bool> _isConnectionPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isRealTimeTestPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isFirmwareUpdatePageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isSettingsPageVisible;
    private NavItem _selectedItem;

    public NavItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            this.RaisePropertyChanged(nameof(CurrentPage));
        }
    }

    public MainWindowViewModel(ConnectionViewModel connectionViewModel,
        RealTimeTestViewModel realTimeTestViewModel,
        FirmwareUpdateViewModel firmwareUpdateViewModel,
        SettingsViewModel settingsViewModel,
        StatusBarViewModel statusBarViewModel)
    {
        ConnectionViewModel = connectionViewModel;
        RealTimeTestViewModel = realTimeTestViewModel;
        FirmwareUpdateViewModel = firmwareUpdateViewModel;
        SettingsViewModel = settingsViewModel;
        StatusBarViewModel = statusBarViewModel;

        NavItems = new List<NavItem>
        {
            new("设备管理", ConnectionViewModel, "ViewAll"),
            new("实时测试", RealTimeTestViewModel, "Play"),
            new("固件升级", FirmwareUpdateViewModel, "Sync")
        };

        SelectedItem = NavItems[0];


        FooterNavItems = new List<NavItem> { new("设置", SettingsViewModel, "Setting") };

        _isConnectionPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == ConnectionViewModel)
            .ToProperty(this, x => x.IsConnectionPageVisible);

        _isRealTimeTestPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == RealTimeTestViewModel)
            .ToProperty(this, x => x.IsRealTimeTestPageVisible);

        _isFirmwareUpdatePageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == FirmwareUpdateViewModel)
            .ToProperty(this, x => x.IsFirmwareUpdatePageVisible);

        _isSettingsPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == SettingsViewModel)
            .ToProperty(this, x => x.IsSettingsPageVisible);
    }

    public IReadOnlyList<NavItem> NavItems { get; }

    public IReadOnlyList<NavItem> FooterNavItems { get; set; }

    public object CurrentPage => SelectedItem?.Page;

    public bool IsConnectionPageVisible => _isConnectionPageVisible?.Value ?? false;
    public bool IsRealTimeTestPageVisible => _isRealTimeTestPageVisible?.Value ?? false;
    public bool IsFirmwareUpdatePageVisible => _isFirmwareUpdatePageVisible?.Value ?? false;
    public bool IsSettingsPageVisible => _isSettingsPageVisible?.Value ?? false;

    public ConnectionViewModel ConnectionViewModel { get; }
    public RealTimeTestViewModel RealTimeTestViewModel { get; }
    public FirmwareUpdateViewModel FirmwareUpdateViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public StatusBarViewModel StatusBarViewModel { get; }

    public record NavItem(string Title, object Page, object Icon);
}