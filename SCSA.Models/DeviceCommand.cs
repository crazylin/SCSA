namespace SCSA.Models;

/// <summary>
///     操作指令枚举
/// </summary>
public enum DeviceCommand : byte
{
    /// <summary>
    ///     0x00 开始采集
    /// </summary>
    RequestStartCollection = 0x00,
    ReplyStartCollection = 0x01,

    /// <summary>
    ///     0x01 停止采集
    /// </summary>
    RequestStopCollection = 0x02,
    ReplyStopCollection = 0x03,

    /// <summary>
    ///     0x02 数据上传
    /// </summary>
    ReplyUploadData = 0x04,

    /// <summary>
    ///     0x03 设置参数
    /// </summary>
    RequestSetParameters = 0x06,
    ReplySetParameters = 0x07,

    /// <summary>
    ///     0x04 读取参数
    /// </summary>
    RequestReadParameters = 0x08,
    ReplyReadParameters = 0x09,

    /// <summary>
    ///     0x0A 获取设备参数Id列表
    /// </summary>
    RequestGetParameterIds = 0x0A,
    ReplyGetParameterIds = 0x0B,

    /// <summary>
    ///     0x0C 获取设备状态
    /// </summary>
    RequestGetDeviceStatus = 0x0C,
    ReplyGetDeviceStatus = 0x0D,

    // -----------------------------------------------------------------
    // 固件升级相关命令(Bootloader 模式)
    // -----------------------------------------------------------------

    /// <summary>
    /// 0xF8 PC -> Device : 启动升级
    /// </summary>
    RequestStartFirmwareUpgrade = 0xF8,

    /// <summary>
    /// 0xF9 Device -> PC : 启动升级应答
    /// </summary>
    ReplyStartFirmwareUpgrade = 0xF9,

    /// <summary>
    /// 0xFA Device -> PC : 升级请求
    /// </summary>
    DeviceRequestFirmwareUpgrade = 0xFA,

    /// <summary>
    /// 0xFB PC -> Device : 发送固件参数(CRC 加密值 + 大小)
    /// </summary>
    RequestSendFirmwareInfo = 0xFB,

    /// <summary>
    /// 0xFC Device -> PC : 固件参数应答
    /// </summary>
    ReplySendFirmwareInfo = 0xFC,

    /// <summary>
    /// 0xFD PC -> Device : 传输固件数据包
    /// </summary>
    RequestTransferFirmwareUpgrade = 0xFD,

    /// <summary>
    /// 0xFE Device -> PC : 传输固件数据包应答
    /// </summary>
    ReplyTransferFirmwareUpgrade = 0xFE,

    /// <summary>
    /// 0xFF Device -> PC : 升级结果
    /// </summary>
    ReplyFirmwareUpgradeResult = 0xFF,

    // -----------------------------------------------------------------
    // 脉冲输出控制命令
    // -----------------------------------------------------------------

    /// <summary>
    /// 0xD0 PC -> Device : 开始脉冲输出
    /// </summary>
    RequestStartPulseOutput = 0xD0,

    /// <summary>
    /// 0xD1 Device -> PC : 开始脉冲输出应答
    /// </summary>
    ReplyStartPulseOutput = 0xD1,

    /// <summary>
    /// 0xD2 PC -> Device : 停止脉冲输出
    /// </summary>
    RequestStopPulseOutput = 0xD2,

    /// <summary>
    /// 0xD3 Device -> PC : 停止脉冲输出应答
    /// </summary>
    ReplyStopPulseOutput = 0xD3
}