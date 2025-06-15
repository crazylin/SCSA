using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace SCSA.ViewModels;

public class FirmwareUpdateViewModel : ViewModelBase
{
    private readonly ConnectionViewModel _connectionVm;

    private string _buttonText = "开始升级";

    private bool _controlEnable;
    private CancellationTokenSource _cts;
    private string _currentVersion = "1.0.0";

    private double _maxPercentage = 100;

    private string _newVersion = "1.1.0";

    private double _progressPercentage;

    private string _selectedFilePath;

    private string _statusMessage = "准备就绪";

    public FirmwareUpdateViewModel(ConnectionViewModel connectionViewModel)
    {
        _connectionVm = connectionViewModel;
        _connectionVm.WhenAnyValue(x => x.SelectedDevice)
            .Subscribe(device => ControlEnable = device != null);

        var canStart = this.WhenAnyValue(x => x.ControlEnable);
        StartUpgradeCommand = ReactiveCommand.CreateFromTask(StartOrCancelUpgradeAsync, canStart);
        BrowseCommand = ReactiveCommand.CreateFromTask<Control>(BrowseAsync);
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        set => this.RaiseAndSetIfChanged(ref _currentVersion, value);
    }

    public string NewVersion
    {
        get => _newVersion;
        set => this.RaiseAndSetIfChanged(ref _newVersion, value);
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
    }

    public double MaxPercentage
    {
        get => _maxPercentage;
        set => this.RaiseAndSetIfChanged(ref _maxPercentage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string ButtonText
    {
        get => _buttonText;
        set => this.RaiseAndSetIfChanged(ref _buttonText, value);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => this.RaiseAndSetIfChanged(ref _selectedFilePath, value);
    }

    public bool ControlEnable
    {
        get => _controlEnable;
        set => this.RaiseAndSetIfChanged(ref _controlEnable, value);
    }

    public byte[] FirmwareData { get; private set; }

    public ReactiveCommand<Unit, Unit> StartUpgradeCommand { get; }
    public ReactiveCommand<Control, Unit> BrowseCommand { get; }

    private async Task BrowseAsync(Control control)
    {
        if (control == null) return;
        var top = TopLevel.GetTopLevel(control);
        var provider = top?.StorageProvider;
        if (provider == null) return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("固件文件") { Patterns = new[] { "*.bin", "*.hex" } } }
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        SelectedFilePath = path;
        NewVersion = ParseFirmwareVersion(path);
    }

    private async Task StartOrCancelUpgradeAsync()
    {
        if (ButtonText == "取消升级")
        {
            _cts?.Cancel();
            return;
        }

        if (_connectionVm.SelectedDevice == null)
        {
            StatusMessage = "请先连接设备";
            return;
        }

        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            StatusMessage = "请先选择固件文件";
            return;
        }

        ButtonText = "取消升级";
        _cts = new CancellationTokenSource();
        try
        {
            await PerformFirmwareUpdate(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "升级已取消";
        }
        finally
        {
            ButtonText = "开始升级";
        }
    }

    private async Task PerformFirmwareUpdate(CancellationToken token)
    {
        if (!await _connectionVm.SelectedDevice.DeviceControlApi.FirmwareUpgradeStart(FirmwareData.Length,
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
        {
            StatusMessage = "开始升级失败！";
            return;
        }

        var perLen = 1280;
        for (var offset = 0; offset <= FirmwareData.Length; offset += perLen)
        {
            token.ThrowIfCancellationRequested();
            ProgressPercentage = offset;
            StatusMessage = $"升级中... {offset}/{MaxPercentage}";
            var chunk = FirmwareData.Skip(offset).Take(perLen).ToArray();
            var pkgId = offset / perLen + 1;
            if (!await _connectionVm.SelectedDevice.DeviceControlApi.FirmwareUpgradeTransfer(pkgId, chunk,
                    new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
            {
                StatusMessage = "数据传输异常！";
                return;
            }
        }

        StatusMessage = "升级完成！";
    }

    private string ParseFirmwareVersion(string path)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            FirmwareData = File.ReadAllBytes(path);
            MaxPercentage = FirmwareData.Length;
            return fileName.Split('_').LastOrDefault() ?? "未知版本";
        }
        catch
        {
            return "版本解析失败";
        }
    }
}