using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Avalonia.Media;
using SCSA.Utils;
using System.Collections.Generic;
using SCSA.Models;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;

namespace SCSA.ViewModels;

public class FirmwareUpdateViewModel : ViewModelBase
{
    private readonly ConnectionViewModel _connectionVm;

    private CancellationTokenSource _cts;

    [Reactive] public string ButtonText { get; set; } = "开始升级";

    [Reactive] public bool ControlEnable { get; set; }

    [Reactive] public string CurrentVersion { get; set; } = "1.0.0";

    [Reactive] public double MaxPercentage { get; set; } = 100;

    [Reactive] public string NewVersion { get; set; } = "1.1.0";

    [Reactive] public double ProgressPercentage { get; set; }

    [Reactive] public string SelectedFilePath { get; set; }

    [Reactive] public string StatusMessage { get; set; } = "准备就绪";

    [Reactive] public IBrush StatusColor { get; private set; } = Brushes.Black;

    public FirmwareUpdateViewModel(ConnectionViewModel connectionViewModel)
    {
        _connectionVm = connectionViewModel;
        _connectionVm.WhenAnyValue(x => x.SelectedDevice)
            .Subscribe(device => ControlEnable = device != null);

        var canStart = this.WhenAnyValue(x => x.ControlEnable);
        StartUpgradeCommand = ReactiveCommand.CreateFromTask(StartOrCancelUpgradeAsync, canStart);
        BrowseCommand = ReactiveCommand.CreateFromTask<Control>(BrowseAsync);

        // Update color based on status text
        this.WhenAnyValue(x => x.StatusMessage)
            .Subscribe(msg =>
            {
                if (string.IsNullOrEmpty(msg))
                {
                    StatusColor = Brushes.Black;
                }
                else if (msg.Contains("失败") || msg.Contains("异常"))
                {
                    StatusColor = Brushes.Red;
                }
                else if (msg.Contains("完成"))
                {
                    StatusColor = Brushes.Green;
                }
                else if (msg.Contains("取消"))
                {
                    StatusColor = Brushes.Orange;
                }
                else
                {
                    StatusColor = Brushes.Black;
                }
            });
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
    int perLen = 1280;
    private async Task PerformFirmwareUpdate(CancellationToken token)
    {
        // 1. 启动升级
        if (!await _connectionVm.SelectedDevice.DeviceControlApi.FirmwareUpgradeStart(
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
        {
            StatusMessage = "启动升级失败！";
            return;
        }

        // 主动断开旧连接，确保后续使用新连接
        try
        {
            await _connectionVm.DisconnectCommand.Execute(_connectionVm.SelectedDevice);
        }
        catch
        {
            // 忽略异常，继续流程
        }
        StatusMessage = "等待设备进入升级模式...";

        var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {

            //等待设备重新连接
            while (_connectionVm.SelectedDevice == null)
            {
                await Task.Delay(200, token);
                waitCts.Token.ThrowIfCancellationRequested();
            }

            var newDevice = _connectionVm.SelectedDevice;

            // b) 监听 0xFA
            if (!await newDevice.DeviceControlApi.WaitForDeviceRequestFirmwareUpgrade(waitCts.Token))
            {
                StatusMessage = "未收到设备升级请求！";
                return;
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "等待设备升级模式超时！";
            return;
        }

        // 3. 发送固件信息
        if (!await _connectionVm.SelectedDevice.DeviceControlApi.FirmwareUpgradeSendInfo(FirmwareData,
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
        {
            StatusMessage = "固件信息发送失败！";
            return;
        }

        // 4. 同步模式：逐包发送固件数据
        for (var offset = 0; offset < FirmwareData.Length; offset += perLen)
        {
            token.ThrowIfCancellationRequested();
            var chunk = FirmwareData.Skip(offset).Take(perLen).ToArray();
            var pkgId = offset / perLen + 1;

            ProgressPercentage = offset + chunk.Length;
            StatusMessage = $"升级中... {ProgressPercentage}/{MaxPercentage}";

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