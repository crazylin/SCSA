using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SCSA.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<bool> _isConnectionPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isRealTimeTestPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isFirmwareUpdatePageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isSettingsPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isPlaybackPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isDebugParameterPageVisible;
    private readonly ObservableAsPropertyHelper<bool> _isPulseOutputPageVisible;
    private readonly ObservableAsPropertyHelper<string> _windowTitle;
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
        DebugParameterViewModel debugParameterViewModel,
        FirmwareUpdateViewModel firmwareUpdateViewModel,
        PlaybackViewModel playbackViewModel,
        SettingsViewModel settingsViewModel,
        StatusBarViewModel statusBarViewModel,
        PulseOutputViewModel pulseOutputViewModel)
    {
        ConnectionViewModel = connectionViewModel;
        RealTimeTestViewModel = realTimeTestViewModel;
        DebugParameterViewModel = debugParameterViewModel;
        FirmwareUpdateViewModel = firmwareUpdateViewModel;
        PlaybackViewModel = playbackViewModel;
        SettingsViewModel = settingsViewModel;
        StatusBarViewModel = statusBarViewModel;
        PulseOutputViewModel = pulseOutputViewModel;

        NavItems = new List<NavItem>
        {
            new("设备管理", ConnectionViewModel, "ViewAll"),
            new("实时测试", RealTimeTestViewModel, "Play"),
            new("调试参数", DebugParameterViewModel, "Code"),
            new("脉冲输出", PulseOutputViewModel, "Pulse"),
            //new("数据回放", PlaybackViewModel, "Replay"),
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

        _isPlaybackPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == PlaybackViewModel)
            .ToProperty(this, x => x.IsPlaybackPageVisible);

        _isDebugParameterPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == DebugParameterViewModel)
            .ToProperty(this, x => x.IsDebugParameterPageVisible);

        _isPulseOutputPageVisible = this.WhenAnyValue(x => x.CurrentPage)
            .Select(page => page == PulseOutputViewModel)
            .ToProperty(this, x => x.IsPulseOutputPageVisible);

        // Window title binding
        _windowTitle = this.WhenAnyValue(x => x.SelectedItem)
            .Select(item => item == null ? "SCSA" : $"SCSA - {item.Title}")
            .ToProperty(this, x => x.WindowTitle);
    }

    public IReadOnlyList<NavItem> NavItems { get; }

    public IReadOnlyList<NavItem> FooterNavItems { get; set; }

    public object CurrentPage => SelectedItem?.Page;

    public bool IsConnectionPageVisible => _isConnectionPageVisible?.Value ?? false;
    public bool IsRealTimeTestPageVisible => _isRealTimeTestPageVisible?.Value ?? false;
    public bool IsFirmwareUpdatePageVisible => _isFirmwareUpdatePageVisible?.Value ?? false;
    public bool IsSettingsPageVisible => _isSettingsPageVisible?.Value ?? false;
    public bool IsPlaybackPageVisible => _isPlaybackPageVisible?.Value ?? false;
    public bool IsDebugParameterPageVisible => _isDebugParameterPageVisible?.Value ?? false;
    public bool IsPulseOutputPageVisible => _isPulseOutputPageVisible?.Value ?? false;

    public string WindowTitle => _windowTitle?.Value ?? "SCSA";

    public ConnectionViewModel ConnectionViewModel { get; }
    public RealTimeTestViewModel RealTimeTestViewModel { get; }
    public DebugParameterViewModel DebugParameterViewModel { get; }
    public FirmwareUpdateViewModel FirmwareUpdateViewModel { get; }
    public PlaybackViewModel PlaybackViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public StatusBarViewModel StatusBarViewModel { get; }
    public PulseOutputViewModel PulseOutputViewModel { get; }

    public record NavItem(string Title, object Page, object Icon);
}