using System;
using System.Collections.Generic;
using System.Net;
using ReactiveUI;
using SCSA.IO.Net.TCP;
using SCSA.Services.Device;

namespace SCSA.Models;

public class DeviceConnection : ReactiveObject
{
    public string DeviceId { get; set; } = "未知";
    public string FirmwareVersion { set; get; } = "未知";
    public IPEndPoint EndPoint { get; set; }
    public DateTime ConnectTime { get; set; }

    public PipelineTcpClient<PipelineNetDataPackage> Client { set; get; }

    public List<DeviceParameter> DeviceParameters { set; get; }

    public PipelineDeviceControlApiAsync DeviceControlApi { set; get; }
}