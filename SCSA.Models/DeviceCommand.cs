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
    ///     0xFF 固件升级
    /// </summary>
    RequestStartFirmwareUpgrade = 0xFC,
    ReplyStartFirmwareUpgrade = 0xFD,
    RequestTransferFirmwareUpgrade = 0xFE,
    ReplyTransferFirmwareUpgrade = 0xFF
}