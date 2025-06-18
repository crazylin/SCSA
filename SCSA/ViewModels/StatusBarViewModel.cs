using ReactiveUI.Fody.Helpers;
using SCSA.ViewModels;

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
        [Reactive] public string? TestRunningTime { get; set; }
        [Reactive] public bool IsTestRunning { get; set; }
        [Reactive] public bool ShowDataStorageInfo { get; set; }
        [Reactive] public bool ShowTriggerStatus { get; set; }
    }
} 