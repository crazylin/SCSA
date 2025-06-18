using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.IO.Net.TCP;
using SCSA.Services.Device;

namespace SCSA.Models;

public class DeviceConnection : ReactiveObject
{
    [Reactive] public string DeviceId { get; set; } = "未知";
    [Reactive] public string FirmwareVersion { get; set; } = "未知";
    [Reactive] public IPEndPoint EndPoint { get; set; }
    [Reactive] public DateTime ConnectTime { get; set; }

    [Reactive] public PipelineTcpClient<PipelineNetDataPackage> Client { get; set; }

    [Reactive] public List<DeviceParameter> DeviceParameters { get; set; }

    [Reactive] public PipelineDeviceControlApiAsync DeviceControlApi { get; set; }

    public List<ParameterType> SupportParameterTypes => DefaultParameters();


    public static List<ParameterType> DefaultParameters()
    {
        var list = Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>().ToList();

        list.Remove(ParameterType.TriggerSampleMode);
        list.Remove(ParameterType.TriggerSampleLevel);
        list.Remove(ParameterType.TriggerSampleChannel);
        list.Remove(ParameterType.TriggerSampleLength);
        list.Remove(ParameterType.TriggerSampleDelay);

        return list;
    }
}