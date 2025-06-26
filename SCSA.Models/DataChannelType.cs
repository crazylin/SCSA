using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    public enum DataChannelType : byte
    {
        Velocity = 0x00,
        Displacement = 0x01,
        Acceleration = 0x02,
        ISignalAndQSignal = 0x03
    }
}
