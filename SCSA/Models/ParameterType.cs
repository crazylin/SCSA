using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Models
{
    public enum ParameterType : uint
    {
        /// <summary>
        /// 0x00000000 | 1字节 | 采样率
        /// </summary>
        SamplingRate = 0x00000000,

        /// <summary>
        /// 0x00000001 | 4字节 | 上传数据类型
        /// </summary>
        UploadDataType = 0x00000001,

        /// <summary>
        /// 0x00000002 | 1字节 | 激光功率
        /// </summary>
        LaserPower = 0x00000002,

        /// <summary>
        /// 0x00000003 | 1字节 | 信号强度
        /// </summary>
        SignalStrength = 0x00000003,

        ///// <summary>
        ///// 0x00000004 | N字节 | 硬件信息
        ///// </summary>
        //HardwareInfo = 0x00000004,

        /// <summary>
        /// 0x00000005 | 1字节 | 低通滤波
        /// </summary>
        LowPassFilter = 0x00000005,

        /// <summary>
        /// 0x00000006 | 1字节 | 高通滤波
        /// </summary>
        HighPassFilter = 0x00000006,

        /// <summary>
        /// 0x00000007 | 1字节 | 速度量程
        /// </summary>
        VelocityRange = 0x00000007,

        /// <summary>
        /// 0x00000008 | 1字节 | 位移量程
        /// </summary>
        DisplacementRange = 0x00000008,

        /// <summary>
        /// 0x00000009 | 1字节 | 加速度量程
        /// </summary>
        AccelerationRange = 0x00000009,

        /// <summary>
        /// 0x0000000A | 4字节 | 模拟输出类型
        /// </summary>
        AnalogOutputType1 = 0x0000000A,

        /// <summary>
        /// 0x0000000B | 1字节 | 模拟输出开关
        /// </summary>
        AnalogOutputSwitch1 = 0x0000000B,
        /// <summary>
        /// 0x0000000A | 4字节 | 模拟输出类型
        /// </summary>
        AnalogOutputType2 = 0x0000000C,

        /// <summary>
        /// 0x0000000B | 1字节 | 模拟输出开关
        /// </summary>
        AnalogOutputSwitch2 = 0x0000000D,
        /// <summary>
        /// 0x0000000C | 可变长度 | 触发采样（待定）
        /// </summary>
        //TriggerSampling = 0x0000000C,

        /*
         * 算法参数范围（0x10000000~0x20000000）
         * 具体定义待算法定型后补充
         */


        FrontFilter = 0x10000000

    }
}
