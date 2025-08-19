using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SCSA.Models;

public class Parameter
{


    public ParameterType Address { set; get; }

    public int Length { set; get; }
    public object Value { set; get; }

    public byte[] RawValue { set; get; }


    public byte[] GetParameterData()
    {
        switch (Address)
        {
            case ParameterType.SamplingRate:
            case ParameterType.UploadDataType:
                return [(byte)Value];
            case ParameterType.LaserPowerIndicatorLevel:
                return [Convert.ToByte(Value)];
            case ParameterType.SignalStrength:
            case ParameterType.LowPassFilter:
            case ParameterType.HighPassFilter:
            case ParameterType.VelocityRange:
            case ParameterType.DisplacementRange:
            case ParameterType.AccelerationRange:
            case ParameterType.DigitalRange:
            case ParameterType.AnalogOutputType1:
            case ParameterType.AnalogOutputSwitch1:
            case ParameterType.AnalogOutputType2:
            case ParameterType.AnalogOutputSwitch2:
            case ParameterType.FrontendFilter:
            case ParameterType.FrontendFilterType:
            case ParameterType.FrontendFilterSwitch:
            case ParameterType.FrontendDcRemovalSwitch:
            case ParameterType.OrthogonalityCorrectionSwitch:
            case ParameterType.OrthogonalityCorrectionMode:
                return [(byte)Value];
            case ParameterType.DataSegmentLength:
                return BitConverter.GetBytes(Convert.ToInt32(Value));
            case ParameterType.VelocityLowPassFilterSwitch:
            case ParameterType.DisplacementLowPassFilterSwitch:
            case ParameterType.AccelerationLowPassFilterSwitch:
                return [(byte)Value];
            case ParameterType.VelocityAmpCorrection:
            case ParameterType.DisplacementAmpCorrection:
            case ParameterType.AccelerationAmpCorrection:
                return BitConverter.GetBytes(Convert.ToSingle(Value));
            case ParameterType.TriggerSampleType:
            case ParameterType.TriggerSampleMode:
                return [(byte)Value];
            case ParameterType.TriggerSampleLevel:
                return BitConverter.GetBytes(Convert.ToSingle(Value));
            case ParameterType.TriggerSampleChannel:
                return [(byte)Value];
            case ParameterType.TriggerSampleLength:
            case ParameterType.TriggerSampleDelay:
                return BitConverter.GetBytes(Convert.ToInt32(Value));
            case ParameterType.LaserDriveCurrent:
            case ParameterType.TECTargetTemperature:
            case ParameterType.OrthogonalityCorrectionValue:
                return BitConverter.GetBytes(Convert.ToSingle(Value));
            default:
                return [0];
        }
    }

    public static List<EnumOption> GetSampleOptions()
    {
        return new List<EnumOption>
        {
            new("2kHz", (byte)0x00),
            new("5kHz", (byte)0x01),
            new("10kHz", (byte)0x02),
            new("20kHz", (byte)0x03),
            new("50kHz", (byte)0x04),
            new("100kHz", (byte)0x05),
            new("200kHz", (byte)0x06),
            new("400kHz", (byte)0x07),
            new("800kHz", (byte)0x08),
            new("1MHz", (byte)0x09),
            new("2MHz", (byte)0x0A),
            new("4MHz", (byte)0x0B),
            new("5MHz", (byte)0x0C),
            new("10MHz", (byte)0x0D),
            new("20MHz", (byte)0x0E)
        };
    }

    public static List<EnumOption> GetLowPassOptions()
    {
        return new List<EnumOption>
        {
            new("100Hz", (byte)0x00),
            new("500Hz", (byte)0x01),
            new("1kHz", (byte)0x02),
            new("2kHz", (byte)0x03),
            new("5kHz", (byte)0x04),
            new("10kHz", (byte)0x05),
            new("20kHz", (byte)0x06),
            new("40kHz", (byte)0x07),
            new("80kHz", (byte)0x08),
            new("100kHz", (byte)0x09),
            new("160kHz", (byte)0x0A),
            new("320kHz", (byte)0x0B),
            new("500kHz", (byte)0x0C),
            new("1MHz", (byte)0x0D),
            new("3MHz", (byte)0x0E)
        };
    }

    public static Type GetParameterType(ParameterType parameterType)
    {
        switch (parameterType)
        {
            case ParameterType.SamplingRate:
            case ParameterType.UploadDataType:
            case ParameterType.LaserPowerIndicatorLevel:
            case ParameterType.SignalStrength:
            case ParameterType.LowPassFilter:
            case ParameterType.HighPassFilter:
            case ParameterType.VelocityRange:
            case ParameterType.DigitalRange:
            case ParameterType.DisplacementRange:
            case ParameterType.AccelerationRange:
            case ParameterType.AnalogOutputType1:
            case ParameterType.AnalogOutputSwitch1:
            case ParameterType.AnalogOutputType2:
            case ParameterType.AnalogOutputSwitch2:
            case ParameterType.FrontendFilter:
            case ParameterType.FrontendFilterType:
            case ParameterType.FrontendFilterSwitch:
            case ParameterType.FrontendDcRemovalSwitch:
            case ParameterType.OrthogonalityCorrectionSwitch:
            case ParameterType.OrthogonalityCorrectionMode:
                return typeof(byte);
            case ParameterType.DataSegmentLength:
                return typeof(int);
            case ParameterType.VelocityLowPassFilterSwitch:
            case ParameterType.DisplacementLowPassFilterSwitch:
            case ParameterType.AccelerationLowPassFilterSwitch:
                return typeof(byte);
            case ParameterType.VelocityAmpCorrection:
            case ParameterType.DisplacementAmpCorrection:
            case ParameterType.AccelerationAmpCorrection:
                return typeof(float);
            case ParameterType.TriggerSampleType:
            case ParameterType.TriggerSampleMode:
                return typeof(byte);
            case ParameterType.TriggerSampleLevel:
                return typeof(float);
            case ParameterType.TriggerSampleChannel:
                return typeof(byte);
            case ParameterType.TriggerSampleLength:
            case ParameterType.TriggerSampleDelay:
                return typeof(int);
            case ParameterType.LaserDriveCurrent:
            case ParameterType.TECTargetTemperature:
            case ParameterType.OrthogonalityCorrectionValue:
                return typeof(float);

            default:
                return typeof(byte);
        }
    }

    public static int GetParameterLength(ParameterType parameterType)
    {
        var t = GetParameterType(parameterType);

        // 验证类型是否为值类型
        if (!t.IsValueType) throw new ArgumentException("Type must be a value type.");

        var size = Marshal.SizeOf(t);
        return size;
    }

    //public static byte[] ToBytes(Parameter parameter,object obj)
    //{
    //    if (parameter.Length == 1)
    //    {
    //        if (obj is byte b)
    //            return [b];
    //        else if (obj is Boolean boo)
    //            return [boo ? (byte)1 : (byte)0];
    //    }
    //    if (obj is Int32 i32)
    //        return BitConverter.GetBytes(i32);
    //    else if (obj is UInt32 ui32)
    //        return BitConverter.GetBytes(ui32);
    //    else if (obj is Int16 i16)
    //        return BitConverter.GetBytes(i16);
    //    else if (obj is UInt16 ui16)
    //        return BitConverter.GetBytes(ui16);
    //    else if (obj is byte b)
    //        return [b];
    //    else if (obj is string str)
    //        return Encoding.ASCII.GetBytes(str);
    //    else if (obj is float fl)
    //        return BitConverter.GetBytes(fl);
    //    else if (obj is double dou)
    //        return BitConverter.GetBytes(dou);
    //    else if (obj is Boolean boo)
    //        return [boo ? (byte)1 : (byte)0];
    //    else if (obj is DataChannelType dct)
    //        return [(byte)dct];
    //    return [];
    //}
    public object ToParameterData()
    {
        switch (Address)
        {
            case ParameterType.SamplingRate:
            case ParameterType.UploadDataType:
            case ParameterType.LaserPowerIndicatorLevel:
            case ParameterType.SignalStrength:
            case ParameterType.LowPassFilter:
            case ParameterType.HighPassFilter:
            case ParameterType.VelocityRange:
            case ParameterType.DisplacementRange:
            case ParameterType.AccelerationRange:
            case ParameterType.DigitalRange:
            case ParameterType.AnalogOutputType1:
            case ParameterType.AnalogOutputSwitch1:
            case ParameterType.AnalogOutputType2:
            case ParameterType.AnalogOutputSwitch2:
            case ParameterType.FrontendFilter:
            case ParameterType.FrontendFilterType:
            case ParameterType.FrontendFilterSwitch:
            case ParameterType.FrontendDcRemovalSwitch:
            case ParameterType.OrthogonalityCorrectionSwitch:
            case ParameterType.OrthogonalityCorrectionMode:
                return RawValue[0];
            case ParameterType.DataSegmentLength:
                return BitConverter.ToInt32(RawValue);
            case ParameterType.VelocityLowPassFilterSwitch:
            case ParameterType.DisplacementLowPassFilterSwitch:
            case ParameterType.AccelerationLowPassFilterSwitch:
                return RawValue[0];
            case ParameterType.VelocityAmpCorrection:
            case ParameterType.DisplacementAmpCorrection:
            case ParameterType.AccelerationAmpCorrection:
                return BitConverter.ToSingle(RawValue);
            case ParameterType.TriggerSampleType:
            case ParameterType.TriggerSampleMode:
                return RawValue[0];
            case ParameterType.TriggerSampleLevel:
                return BitConverter.ToSingle(RawValue);
            case ParameterType.TriggerSampleChannel:
                return RawValue[0];
            case ParameterType.TriggerSampleLength:
            case ParameterType.TriggerSampleDelay:
                return BitConverter.ToInt32(RawValue);
            case ParameterType.LaserDriveCurrent:
            case ParameterType.TECTargetTemperature:
            case ParameterType.OrthogonalityCorrectionValue:
                return BitConverter.ToSingle(RawValue);

            default:
                return RawValue[0];
        }
    }



    public static List<Parameter> Get_GetParametersResult(byte[] data)
    {
        var parameters = new List<Parameter>();
        var reader = new BinaryReader(new MemoryStream(data));
        var len = reader.ReadInt32();
        for (var i = 0; i < len; i++)
        {
            var parameter = new Parameter();
            var bytes = reader.ReadBytes(4);
            parameter.Address = (ParameterType)BitConverter.ToInt32(bytes);
            parameter.Length = reader.ReadInt32();
            parameter.RawValue = reader.ReadBytes(parameter.Length);
            parameters.Add(parameter);
        }

        return parameters;
    }

    public static byte[] Get_SetParametersData(List<Parameter> parameters)
    {
        var bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(parameters.Count));
        foreach (var parameter in parameters)
        {
            bytes.AddRange(BitConverter.GetBytes((int)parameter.Address));
            bytes.AddRange(BitConverter.GetBytes(parameter.Length));
            bytes.AddRange(parameter.GetParameterData());
        }

        return bytes.ToArray();
    }

    public static byte[] Get_GetParametersData(List<Parameter> parameters)
    {
        var bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(parameters.Count));
        foreach (var parameter in parameters) bytes.AddRange(BitConverter.GetBytes((int)parameter.Address));

        return bytes.ToArray();
    }

    public static List<Parameter> Get_GetParameterIdsResult(byte[] data)
    {
        var parameters = new List<Parameter>();
        var reader = new BinaryReader(new MemoryStream(data));
        var len = reader.ReadInt32();
        for (var i = 0; i < len; i++)
        {
            var parameter = new Parameter();
            var bytes = reader.ReadBytes(4);
            parameter.Address = (ParameterType)BitConverter.ToInt32(bytes);
            parameters.Add(parameter);
        }

        return parameters;
    }

    public static double GetSampleRate(byte sampleRate)
    {
        switch (sampleRate)
        {
            case 0x00:
                return 2000;
            case 0x01:
                return 5000;
            case 0x02:
                return 10000;
            case 0x03:
                return 20000;
            case 0x04:
                return 50000;
            case 0x05:
                return 100000;
            case 0x06:
                return 200000;
            case 0x07:
                return 400000;
            case 0x08:
                return 800000;
            case 0x09:
                return 1000000;
            case 0x0A:
                return 2000000;
            case 0x0B:
                return 4000000;
            case 0x0C:
                return 5000000;
            case 0x0D:
                return 10000000;
            case 0x0E:
                return 20000000;
        }

        return 20000000;
    }
}