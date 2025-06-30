using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Models;
using SCSA.ViewModels.Messages;

namespace SCSA.ViewModels;

/// <summary>
/// 专用于激光器电流与 TEC 目标温度调试的 ViewModel。
/// 仅当 <see cref="IsLocked"/> 为 true 时才能下发配置，避免误操作。
/// </summary>
public class DebugParameterViewModel : ViewModelBase
{
    public DebugParameterViewModel()
    {
        // 设备连接状态 + 锁定状态控制按钮可用性
        var canRead = this.WhenAnyValue(x => x.IsDeviceConnected);
        var canSave = this.WhenAnyValue(x => x.IsLocked, x => x.IsDeviceConnected,
            (locked, connected) => locked && connected);

        SaveCommand = ReactiveCommand.Create(Save, canSave);
        ReadCommand = ReactiveCommand.Create(Read, canRead);

        // 订阅设备连接/参数变更
        MessageBus.Current.Listen<SelectedDeviceChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSelectedDeviceChanged);

        MessageBus.Current.Listen<ParametersChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnParametersChanged);
    }

    [Reactive] public float LaserCurrent { get; set; } = 45f; // mA
    [Reactive] public float TECTargetTemperature { get; set; } = 25f; // °C
    [Reactive] public bool IsLocked { get; set; }
    [Reactive] public bool IsDeviceConnected { get; private set; }

    private DeviceConnection? _currentDevice;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ReadCommand { get; }

    private void OnSelectedDeviceChanged(SelectedDeviceChangedMessage msg)
    {
        _currentDevice = msg.Value;
        IsDeviceConnected = _currentDevice != null;
        if (_currentDevice?.DeviceParameters != null)
            UpdateValuesFrom(_currentDevice.DeviceParameters);
    }

    private void OnParametersChanged(ParametersChangedMessage msg)
    {
        if (_currentDevice != null && msg.Value.EndPoint.Equals(_currentDevice.EndPoint))
            UpdateValuesFrom(msg.Value.DeviceParameters);
    }

    private void UpdateValuesFrom(IEnumerable<DeviceParameter> parameters)
    {
        var laser = parameters.FirstOrDefault(p => p.Address == (int)ParameterType.LaserDriveCurrent);
        if (laser != null && laser.Value is float lc)
            LaserCurrent = lc;

        var tec = parameters.FirstOrDefault(p => p.Address == (int)ParameterType.TECTargetTemperature);
        if (tec != null && tec.Value is float tt)
            TECTargetTemperature = tt;
    }

    private void Save()
    {
        var list = new List<Parameter>
        {
            new() { Address = ParameterType.LaserDriveCurrent, Length = Parameter.GetParameterLength(ParameterType.LaserDriveCurrent), Value = LaserCurrent },
            new() { Address = ParameterType.TECTargetTemperature, Length = Parameter.GetParameterLength(ParameterType.TECTargetTemperature), Value = TECTargetTemperature }
        };
        MessageBus.Current.SendMessage(new RequestWriteParametersMessage(list));
        ShowNotification("调试参数已下发", InfoBarSeverity.Success);
    }

    private void Read()
    {
        ShowNotification("正在读取调试参数...");
        MessageBus.Current.SendMessage(new RequestReadParametersMessage());
    }
} 