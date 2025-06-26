using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Models;
using SCSA.ViewModels.Messages;

namespace SCSA.ViewModels
{
    public class StatusBarViewModel : ViewModelBase
    {
        [Reactive] public string? ConnectedDevice { get; set; } = "无连接";
        [Reactive] public string? ListeningEndpoint { get; set; } = "未监听";
        [Reactive] public string? AcquisitionMode { get; set; }
        [Reactive] public string? TriggerMode { get; set; }
        [Reactive] public string? TriggerStatus { get; set; }
        [Reactive] public double ReceivedProgress { get; set; }
        [Reactive] public double SaveProgress { get; set; }
        [Reactive] public string? TestRunningTime { get; set; } = "运行时: 00:00:00";
        [Reactive] public bool IsTestRunning { get; set; }
        [Reactive] public bool ShowDataStorageInfo { get; set; }
        [Reactive] public bool ShowTriggerStatus { get; set; }

        // Device Status Properties
        [Reactive] public Int32 TEC_NTC { get; set; }
        [Reactive] public float BoardTemperature { get; set; }
        [Reactive] public float PdCurrent { get; set; }
        [Reactive] public string RunningState { get; set; }
        [Reactive] public bool ShowDeviceStatus { get; set; }

        public StatusBarViewModel()
        {
            MessageBus.Current.Listen<DeviceStatusChangedMessage>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(message =>
                {
                    if (message.DeviceConnection.DeviceStatuses == null || !message.DeviceConnection.DeviceStatuses.Any())
                    {
                        ShowDeviceStatus = false;
                        return;
                    }

                    ShowDeviceStatus = true;
                    foreach (var status in message.DeviceConnection.DeviceStatuses)
                    {
                        switch (status.Address)
                        {
                            case DeviceStatusType.RunningState:
                                RunningState = Convert.ToByte(status.Value) switch
                                {
                                    0 => $"运行状态: 正常",
                                    1 => $"运行状态: 上传数据中",
                                    2 => $"运行状态: 升级中",
                                    _ => $"运行状态: 异常"
                                };
                                break;
                            case DeviceStatusType.TecNtc:
                                if (status.Value is Int32 tec)
                                    TEC_NTC = tec;
                                break;
                            case DeviceStatusType.BoardTemperature:
                                if (status.Value is float temp)
                                    BoardTemperature = temp;
                                break;
                            case DeviceStatusType.PdCurrent:
                                if (status.Value is float current)
                                    PdCurrent = current;
                                break;
                            
                        }
                    }
                });

            MessageBus.Current.Listen<SelectedDeviceChangedMessage>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(message => ShowDeviceStatus = message.Value != null);
        }
    }
} 