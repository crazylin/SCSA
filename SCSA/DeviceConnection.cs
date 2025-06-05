using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SCSA.IO.Net.TCP;

namespace SCSA.Models
{

    public class DeviceConnection:ObservableObject
    {

        public string DeviceId { get; set; } = "未知";
        public string FirmwareVersion { set; get; } = "未知";
        public IPEndPoint EndPoint { get; set; }
        public DateTime ConnectTime { get; set; }

        public PipelineTcpClient<PipelineNetDataPackage> Client { set; get; }

        public List<DeviceParameter> DeviceParameters { set; get; }

        public PipelineDeviceControlApiAsync DeviceControlApi { set; get; }
    }
}
