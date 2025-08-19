namespace SCSA.Models;

public enum ParameterType : uint
{
    SamplingRate = 0x00000000,
    UploadDataType = 0x00000001,
    LaserPowerIndicatorLevel = 0x00000002,
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

    // 触发采样相关
    TriggerSampleType = 0x0000000E, // 触发采样类型
    TriggerSampleMode = 0x0000000F, // 触发采样-触发采样模式-软（上升沿、下降沿）
    TriggerSampleLevel = 0x00000010, // 触发采样-触发采样模式-硬（触发电平）
    TriggerSampleChannel = 0x00000011, // 触发通道
    TriggerSampleLength = 0x00000012, // 触发采样长度
    TriggerSampleDelay = 0x00000013, // 触发前置点数

    // 新增硬件相关参数
    LaserDriveCurrent = 0x00000014,              // 激光器电流 (mA) float
    TECTargetTemperature = 0x00000015,        // TEC目标温度 (℃) float

    //数字口量程
    DigitalRange = 0x00000016,

    //前端算法
    FrontendFilter = 0x10000000,
    FrontendFilterType = 0x10000001,
    FrontendFilterSwitch = 0x10000002,
    FrontendDcRemovalSwitch = 0x10000003,
    DataSegmentLength = 0x10000005,
    VelocityLowPassFilterSwitch = 0x10000006,
    DisplacementLowPassFilterSwitch = 0x10000007,
    AccelerationLowPassFilterSwitch = 0x10000008,
    VelocityAmpCorrection = 0x10000009,
    DisplacementAmpCorrection = 0x1000000A,
    AccelerationAmpCorrection = 0x1000000B,

    OrthogonalityCorrectionSwitch = 0x10000004,
    OrthogonalityCorrectionMode = 0x1000000C,
    OrthogonalityCorrectionValue = 0x1000000D,
}