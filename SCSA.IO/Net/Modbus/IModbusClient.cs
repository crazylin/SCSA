using System;
using System.Collections.Generic;
using System.Linq;

namespace SCSA.IO.Net.Modbus
{

    public interface IModbusClient
    {
        /// <summary>
        ///     串口名称
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     波特率，默认9600
        /// </summary>
        int BaudRate { get; }

        /// <summary>
        ///     连接状态
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        ///     发送数据
        /// </summary>
        /// <param name="datas"></param>
        void Send(ModbusMessage messgae);

        /// <summary>
        ///     数据接收事件
        /// </summary>
        event EventHandler<ModbusMessage> OnDataReceived;

        /// <summary>
        ///     端口断开事件
        /// </summary>
        event EventHandler OnSessionClosed;

        /// <summary>
        ///     开启端口
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baudRate"></param>
        void Start(string name, int baudRate);

        /// <summary>
        ///     停止端口
        /// </summary>
        void Stop();
    }

    public class ModbusMessage : EventArgs
    {
        public byte Address { set; get; }

        public byte Command { set; get; }

        public short StartAddress { set; get; }

        public short Count { set; get; }

        public byte[] Data { set; get; }

        public byte ErrorCode { set; get; }

        public short Crc { set; get; }

        public bool Extend { set; get; } = false;

        public byte[] ToMessage()
        {
            var data = new List<byte>();
            data.Add(Address);
            data.Add(Command);
            data.AddRange(BitConverter.GetBytes(StartAddress));
            data.AddRange(BitConverter.GetBytes(Count).Reverse());
            //data.AddRange(Data);
            var crc = (short) Crc_Count(data.ToArray());
            data.AddRange(BitConverter.GetBytes(crc));

            return data.ToArray();
        }

        public override string ToString()
        {
            return ToMessage().Select(b => b.ToString("x2")).Aggregate((p, n) => p + " " + n);
        }

        private int Crc_Count(byte[] pbuf)
        {
            var crcbuf = pbuf;
            //计算并填写CRC校验码
            var crc = 0xffff;
            var len = crcbuf.Length;
            for (var n = 0; n < len; n++)
            {
                byte i;
                crc = crc ^ crcbuf[n];
                for (i = 0; i < 8; i++)
                {
                    int TT;
                    TT = crc & 1;
                    crc = crc >> 1;
                    crc = crc & 0x7fff;
                    if (TT == 1) crc = crc ^ 0xa001;

                    crc = crc & 0xffff;
                }
            }

            return crc;
        }
    }
}