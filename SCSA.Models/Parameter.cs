﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SCSA.Utils;

namespace SCSA.Models
{
    public class Parameter
    {
        public ParameterType Address { set; get; }

        public Int32 Length { set; get; }
        public object Value { set; get; }

        public byte[] RawValue { set; get; }

        public bool SetResult { set; get; }

        public bool GetResult { set; get; }

        public byte[] GetParameterData()
        {
            switch (Address)
            {
                case ParameterType.SamplingRate:
                    return [(byte)Value];
                case ParameterType.UploadDataType:
                    return [(byte)Value];
                case ParameterType.LaserPower:
                    return [Convert.ToByte(Value)];
                case ParameterType.SignalStrength:
                    return [(byte)Value];
                case ParameterType.LowPassFilter:
                    return [(byte)Value];
                case ParameterType.HighPassFilter:
                    return [(byte)Value];
                case ParameterType.VelocityRange:
                    return [(byte)Value];
                case ParameterType.DisplacementRange:
                    return [(byte)Value];
                case ParameterType.AccelerationRange:
                    return [(byte)Value];
                case ParameterType.AnalogOutputType1:
                    return [(byte)Value];
                case ParameterType.AnalogOutputSwitch1:
                    return [(byte)Value];
                case ParameterType.AnalogOutputType2:
                    return [(byte)Value];
                case ParameterType.AnalogOutputSwitch2:
                    return [(byte)Value];
                case ParameterType.FrontendFilter:
                    return [(byte)Value];
                case ParameterType.FrontendFilterType:
                    return [(byte)Value];
                case ParameterType.FrontendFilterSwitch:
                    return [(byte)Value];
                case ParameterType.FrontendDcRemovalSwitch:
                    return [(byte)Value];
                case ParameterType.FrontendOrthogonalityCorrectionSwitch:
                    return [(byte)Value];
                case ParameterType.DataSegmentLength:
                    return BitConverter.GetBytes(Convert.ToInt32(Value));
                case ParameterType.VelocityLowPassFilterSwitch:
                    return [(byte)Value];
                case ParameterType.DisplacementLowPassFilterSwitch:
                    return [(byte)Value];
                case ParameterType.AccelerationLowPassFilterSwitch:
                    return [(byte)Value];
                case ParameterType.VelocityAmpCorrection:
                    return BitConverter.GetBytes(Convert.ToSingle(Value));
                case ParameterType.DisplacementAmpCorrection:
                    return BitConverter.GetBytes(Convert.ToSingle(Value));
                case ParameterType.AccelerationAmpCorrection:
                    return BitConverter.GetBytes(Convert.ToSingle(Value));
                default: return [(byte)0];
            }

           
        }

        public static List<EnumOption> GetSampleOptions()
        {
            return new List<EnumOption>()
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
                new("20MHz", (byte)0x0E),
            };
        }

        public static List<EnumOption> GetLowPassOptions()
        {
            return new List<EnumOption>()
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
                new("3MHz", (byte)0x0E),
            };
        }

        public static Type GetParameterType(ParameterType parameterType)
        {
            switch (parameterType)
            {
                case ParameterType.SamplingRate:
                    return typeof(byte);
                case ParameterType.UploadDataType:
                    return typeof(byte);
                case ParameterType.LaserPower:
                    return typeof(byte);
                case ParameterType.SignalStrength:
                    return typeof(byte);
                case ParameterType.LowPassFilter:
                    return typeof(byte);
                case ParameterType.HighPassFilter:
                    return typeof(byte);
                case ParameterType.VelocityRange:
                    return typeof(byte);
                case ParameterType.DisplacementRange:
                    return typeof(byte);
                case ParameterType.AccelerationRange:
                    return typeof(byte);
                case ParameterType.AnalogOutputType1:
                    return typeof(byte);
                case ParameterType.AnalogOutputSwitch1:
                    return typeof(byte);
                case ParameterType.AnalogOutputType2:
                    return typeof(byte);
                case ParameterType.AnalogOutputSwitch2:
                    return typeof(byte);
                case ParameterType.FrontendFilter:
                    return typeof(byte);
                case ParameterType.FrontendFilterType:
                    return typeof(byte);
                case ParameterType.FrontendFilterSwitch:
                    return typeof(byte);
                case ParameterType.FrontendDcRemovalSwitch:
                    return typeof(byte);
                case ParameterType.FrontendOrthogonalityCorrectionSwitch:
                    return typeof(byte);
                case ParameterType.DataSegmentLength:
                    return typeof(Int32);
                case ParameterType.VelocityLowPassFilterSwitch:
                    return typeof(byte);
                case ParameterType.DisplacementLowPassFilterSwitch:
                    return typeof(byte);
                case ParameterType.AccelerationLowPassFilterSwitch:
                    return typeof(byte);
                case ParameterType.VelocityAmpCorrection:
                    return typeof(float);
                case ParameterType.DisplacementAmpCorrection:
                    return typeof(float);
                case ParameterType.AccelerationAmpCorrection:
                    return typeof(float);
                default:
                    return typeof(byte);
            }
        }

        public static int GetParameterLength(ParameterType parameterType)
        {
            var t = GetParameterType(parameterType);

            // 验证类型是否为值类型
            if (!t.IsValueType)
            {
                throw new ArgumentException("Type must be a value type.");
            }

            int size = Marshal.SizeOf(t);
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
                    return RawValue[0];
                case ParameterType.UploadDataType:
                    return RawValue[0];
                case ParameterType.LaserPower:
                    return RawValue[0];
                case ParameterType.SignalStrength:
                    return RawValue[0];
                case ParameterType.LowPassFilter:
                    return RawValue[0];
                case ParameterType.HighPassFilter:
                    return RawValue[0];
                case ParameterType.VelocityRange:
                    return RawValue[0];
                case ParameterType.DisplacementRange:
                    return RawValue[0];
                case ParameterType.AccelerationRange:
                    return RawValue[0];
                case ParameterType.AnalogOutputType1:
                    return RawValue[0];
                case ParameterType.AnalogOutputSwitch1:
                    return RawValue[0];
                case ParameterType.AnalogOutputType2:
                    return RawValue[0];
                case ParameterType.AnalogOutputSwitch2:
                    return RawValue[0];
                case ParameterType.FrontendFilter:
                    return RawValue[0];
                case ParameterType.FrontendFilterType:
                    return RawValue[0];
                case ParameterType.FrontendFilterSwitch:
                    return RawValue[0];
                case ParameterType.FrontendDcRemovalSwitch:
                    return RawValue[0];
                case ParameterType.FrontendOrthogonalityCorrectionSwitch:
                    return RawValue[0];
                case ParameterType.DataSegmentLength:
                    return BitConverter.ToInt32(RawValue);
                case ParameterType.VelocityLowPassFilterSwitch:
                    return RawValue[0];
                case ParameterType.DisplacementLowPassFilterSwitch:
                    return RawValue[0];
                case ParameterType.AccelerationLowPassFilterSwitch:
                    return RawValue[0];
                case ParameterType.VelocityAmpCorrection:
                    return BitConverter.ToSingle(RawValue);
                case ParameterType.DisplacementAmpCorrection:
                    return BitConverter.ToSingle(RawValue);
                case ParameterType.AccelerationAmpCorrection:
                    return BitConverter.ToSingle(RawValue);
                default:
                    return RawValue[0];
            }
        }

        public static List<Parameter> Get_SetParametersResult(byte[] data)
        {
            var parameters = new List<Parameter>();
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            var len = reader.ReadInt32();
            for (int i = 0; i < len; i++)
            {
                var parameter = new Parameter();
                parameter.Address = (ParameterType)reader.ReadInt32();
                parameter.SetResult = reader.ReadInt16() == 0;
                parameters.Add(parameter);
            }

            return parameters;
        }

        public static List<Parameter> Get_GetParametersResult(byte[] data)
        {
            var parameters = new List<Parameter>();
            BinaryReader reader = new BinaryReader(new MemoryStream(data));
            var len = reader.ReadInt32();
            for (int i = 0; i < len; i++)
            {
                var parameter = new Parameter();
                parameter.Address = (ParameterType)reader.ReadInt32();
                parameter.GetResult = reader.ReadInt16() == 0;
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
            foreach (var parameter in parameters)
            {
                bytes.AddRange(BitConverter.GetBytes((int)parameter.Address));
            }

            return bytes.ToArray();

        }

        public static double GetSampleRate(byte sampleRate)
        {
            switch (sampleRate)
            {
                case 0x00:
                    return 2000;
                case 0x01:
                    return 50000;
                case 0x02:
                    return 100000;
                case 0x03:
                    return 200000;
                case 0x04:
                    return 400000;
                case 0x05:
                    return 800000;
                case 0x06:
                    return 1000000;
                case 0x07:
                    return 2000000;
                case 0x08:
                    return 4000000;
                case 0x09:
                    return 8000000;
                case 0x0A:
                    return 16000000;
                case 0x0B:
                    return 20000000;
            }

            return 20000000;
        }





        public enum DataChannelType : byte
        {

            Velocity = 0x00,


            Displacement = 0x01,


            Acceleration = 0x02,


            ISignalAndQSignal = 0x03,
        }

  
    }
}
