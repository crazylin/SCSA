namespace SCSA.Models;

/// <summary>
/// 设备状态ID定义
/// </summary>
public enum DeviceStatusType : uint
{
    /// <summary>
    /// 运行状态
    /// </summary>
    RunningState = 0x00000000,

    /// <summary>
    /// TEC NTTC（Int32，单位是Ω）
    /// </summary>
    TecNtc = 0x00000001,

    /// <summary>
    /// 板卡温度（float，单位是°C)
    /// </summary>
    BoardTemperature = 0x00000002,

    /// <summary>
    /// PD电流（float，单位是mA)
    /// </summary>
    PdCurrent = 0x00000003
} 