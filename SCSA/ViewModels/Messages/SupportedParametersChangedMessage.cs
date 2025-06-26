using System.Collections.Generic;
using SCSA.Models;

namespace SCSA.ViewModels.Messages
{
    public class SupportedParametersChangedMessage
    {
        public DeviceConnection DeviceConnection { get; set; }

        public SupportedParametersChangedMessage(DeviceConnection deviceConnection)
        {
            DeviceConnection = deviceConnection;
        }

    }
} 