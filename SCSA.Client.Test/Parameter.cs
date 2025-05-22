using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Rendering;

namespace SCSA.Client.Test
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
                case ParameterType.FrontFilter:
                    return [(byte)Value];
                default: return [(byte)0];
            }


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
                case ParameterType.FrontFilter:
                    return RawValue[0];
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
