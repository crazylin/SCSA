using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SCSA.Models
{
    public class DeviceStatus
    {
        public DeviceStatusType Address { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public byte[] RawValue { set; get; }
        public string Unit { get; set; }



        public static byte[] Get_GetDeviceStatusData(List<DeviceStatusType> deviceStatusList)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(deviceStatusList.Count));
            foreach (var deviceStatus in deviceStatusList) bytes.AddRange(BitConverter.GetBytes((int)deviceStatus));
            return bytes.ToArray();
        }

        public static List<DeviceStatus> Get_GetDeviceStatusResult(byte[] data)
        {
            var deviceStatusList = new List<DeviceStatus>();
            var reader = new BinaryReader(new MemoryStream(data));
            var len = reader.ReadInt32();
            for (var i = 0; i < len; i++)
            {
                var deviceStatus = new DeviceStatus();
                deviceStatus.Address = (DeviceStatusType)reader.ReadInt32();
                deviceStatus.RawValue = reader.ReadBytes(GetDeviceStatusTypeLength(deviceStatus.Address));
                deviceStatus.Value = deviceStatus.GetDeviceStatusValue();
                deviceStatusList.Add(deviceStatus);
            }

            return deviceStatusList;


        }

        public static Type GetDeviceStatusType(DeviceStatusType deviceStatusType)
        {
            switch (deviceStatusType)
            {
                case DeviceStatusType.RunningState:
                    return typeof(byte);
                case DeviceStatusType.TecNtc:
                    return typeof(Int32);
                case DeviceStatusType.BoardTemperature:
                case DeviceStatusType.PdCurrent:
                    return typeof(float);
                case DeviceStatusType.SignalStrength:
                    return typeof(Int32);

                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceStatusType), deviceStatusType, null);
            }
        }
        public static int GetDeviceStatusTypeLength(DeviceStatusType deviceStatusType)
        {
            var t = GetDeviceStatusType(deviceStatusType);

            // ��֤�����Ƿ�Ϊֵ����
            if (!t.IsValueType) throw new ArgumentException("Type must be a value type.");

            var size = Marshal.SizeOf(t);
            return size;
        }

        public object GetDeviceStatusValue()
        {
            switch (Address)
            {
                case DeviceStatusType.RunningState:
                    return RawValue[0];
                case DeviceStatusType.TecNtc:
                    return BitConverter.ToInt32(RawValue);
                case DeviceStatusType.BoardTemperature:
                case DeviceStatusType.PdCurrent:
                    return BitConverter.ToSingle(RawValue);
                case DeviceStatusType.SignalStrength:
                    return BitConverter.ToInt32(RawValue);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class SignalStrengthValue
        {
            public float Value { get; set; }
            public short Raw1 { get; set; }
            public short Raw2 { get; set; }
        }


        public byte[] GetParameterData()
        {
            switch (Address)
            {
                case DeviceStatusType.RunningState:
                    return [(byte)Value];
                case DeviceStatusType.TecNtc:
                    return BitConverter.GetBytes(Convert.ToInt32(Value));
                case DeviceStatusType.BoardTemperature:
                case DeviceStatusType.PdCurrent:
                    return BitConverter.GetBytes(Convert.ToSingle(Value));
                case DeviceStatusType.SignalStrength:
                    return BitConverter.GetBytes(Convert.ToInt32(Value));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
} 