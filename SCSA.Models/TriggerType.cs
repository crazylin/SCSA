namespace SCSA.Models;

public enum TriggerType : byte
{
    /// <summary>自由触发模式</summary>
    FreeTrigger,

    /// <summary>软件触发模式</summary>
    SoftwareTrigger,

    /// <summary>硬件触发模式</summary>
    HardwareTrigger,

    /// <summary>调试触发模式</summary>
    DebugTrigger
}