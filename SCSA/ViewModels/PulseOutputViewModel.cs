using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Models;
using SCSA.Services;

namespace SCSA.ViewModels;

public class PulseOutputViewModel : ViewModelBase
{
    private readonly ConnectionViewModel _connectionViewModel;
    private readonly IAppSettingsService _settingsService;

    public PulseOutputViewModel(ConnectionViewModel connectionViewModel, IAppSettingsService settingsService)
    {
        _connectionViewModel = connectionViewModel;
        _settingsService = settingsService;

        N_PulseIntervalSeconds = 1.0; // 1秒
        N_PulseCount = 10;
        PulseWidthSeconds = 0.01; // 1秒
        Frequency = 50;

        var canExecuteCommands = this.WhenAnyValue(
            x => x._connectionViewModel.SelectedDevice)
            .Select(device => device != null && device.IsConnected);

        StartPulseCommand = ReactiveCommand.CreateFromTask(StartPulseAsync, canExecuteCommands);
        StopPulseCommand = ReactiveCommand.CreateFromTask(StopPulseAsync, canExecuteCommands);

        // 监听连接状态变化
        this.WhenAnyValue(x => x._connectionViewModel.SelectedDevice)
            .Subscribe(device => UpdateConnectionStatus(device != null && device.IsConnected));
    }

    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public bool IsPulseRunning { get; set; }
    [Reactive] public string StatusMessage { get; set; } = "准备就绪";

    // 界面显示用的属性（以秒为单位）
    [Reactive] public double N_PulseIntervalSeconds { get; set; }
    [Reactive] public long N_PulseCount { get; set; }
    [Reactive] public double PulseWidthSeconds { get; set; }
    [Reactive] public double Frequency { get; set; }

    // 内部使用的纳秒值（用于协议传输）
    private long N_PulseInterval => (long)(N_PulseIntervalSeconds * 1_000_000_000);
    private long PulseWidth => (long)(PulseWidthSeconds * 1_000_000_000);

    public ReactiveCommand<Unit, Unit> StartPulseCommand { get; }
    public ReactiveCommand<Unit, Unit> StopPulseCommand { get; }

    private async Task StartPulseAsync()
    {
        try
        {
            StatusMessage = "正在发送开始脉冲命令...";

            var selectedDevice = _connectionViewModel.SelectedDevice;
            if (selectedDevice?.DeviceControlApi == null)
            {
                throw new InvalidOperationException("设备控制API不可用");
            }

            var success = await selectedDevice.DeviceControlApi.StartPulseOutput(
                N_PulseInterval, N_PulseCount, Frequency, PulseWidth,
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

            if (success)
            {
                IsPulseRunning = true;
                StatusMessage = "脉冲输出已开始";
            }
            else
            {
                StatusMessage = "启动脉冲输出失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
        }
    }

    private async Task StopPulseAsync()
    {
        try
        {
            StatusMessage = "正在发送停止脉冲命令...";
            
            var selectedDevice = _connectionViewModel.SelectedDevice;
            if (selectedDevice?.DeviceControlApi == null)
            {
                throw new InvalidOperationException("设备控制API不可用");
            }

            var success = await selectedDevice.DeviceControlApi.StopPulseOutput(
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

            if (success)
            {
                IsPulseRunning = false;
                StatusMessage = "脉冲输出已停止";
            }
            else
            {
                StatusMessage = "停止脉冲输出失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止失败: {ex.Message}";
        }
    }

    public void UpdateConnectionStatus(bool isConnected)
    {
        IsConnected = isConnected;
        if (!isConnected)
        {
            IsPulseRunning = false;
            StatusMessage = "设备未连接";
        }
        else
        {
            StatusMessage = "设备已连接，准备就绪";
        }
    }
}