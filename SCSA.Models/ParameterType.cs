using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    public enum ParameterType : uint
    {
        SamplingRate = 0x00000000,
        UploadDataType = 0x00000001,
        LaserPower = 0x00000002,
        SignalStrength = 0x00000003,
        ///// <summary>
        ///// 0x00000004 | N字节 | 硬件信息
        ///// </summary>
        //HardwareInfo = 0x00000004,
        LowPassFilter = 0x00000005,
        HighPassFilter = 0x00000006,
        VelocityRange = 0x00000007,
        DisplacementRange = 0x00000008,
        AccelerationRange = 0x00000009,
        AnalogOutputType1 = 0x0000000A,
        AnalogOutputSwitch1 = 0x0000000B,
        AnalogOutputType2 = 0x0000000C,
        AnalogOutputSwitch2 = 0x0000000D,
        FrontendFilter = 0x10000000,
        FrontendFilterType = 0x10000001,
        FrontendFilterSwitch = 0x10000002,
        FrontendDcRemovalSwitch = 0x10000003,
        FrontendOrthogonalityCorrectionSwitch = 0x10000004,
        DataSegmentLength = 0x10000005,
        VelocityLowPassFilterSwitch = 0x10000006,
        DisplacementLowPassFilterSwitch = 0x10000007,
        AccelerationLowPassFilterSwitch = 0x10000008,
        VelocityAmpCorrection = 0x10000009,
        DisplacementAmpCorrection = 0x1000000A,
        AccelerationAmpCorrection = 0x1000000B,
        // 触发采样相关
        TriggerSampleEnable = 0x0000000E, // 触发采样使能
        TriggerSampleMode = 0x0000000F,   // 触发采样模式
        TriggerSampleLevel = 0x00000010,  // 触发电平
        TriggerSampleChannel = 0x00000011,  // 触发通道
        TriggerSampleLength = 0x00000012, // 触发采样长度
        TriggerSampleDelay = 0x00000013, // 触发前置点数
    }
}
