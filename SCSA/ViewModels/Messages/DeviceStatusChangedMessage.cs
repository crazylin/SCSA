using System.Collections.Generic;
using SCSA.Models;

namespace SCSA.ViewModels.Messages
{
    public class DeviceStatusChangedMessage
    {
        public DeviceConnection DeviceConnection { get; set; }

        public DeviceStatusChangedMessage(DeviceConnection deviceConnection)
        {
            DeviceConnection = deviceConnection;
        }
    }
} 