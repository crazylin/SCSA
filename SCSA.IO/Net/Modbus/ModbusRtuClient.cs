using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Markup;

namespace SCSA.IO.Net.Modbus
{

    [Export(typeof(IModbusClient))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ModbusRtuClient : IModbusClient, IDisposable
    {
        private readonly ConcurrentQueue<ModbusMessage> _writeQueue = new();

        private bool _isRunning;
        private Thread _readThread;


        private SerialPort _serialPort;
        private readonly AutoResetEvent _writeLock = new(false);
        private Thread _writeThread;

        public void Dispose()
        {
            Stop();
            _serialPort?.Dispose();
            _writeLock?.Dispose();
        }

        public string Name => "COM1";
        public int BaudRate => 115200;
        public bool IsOpen => _serialPort is {IsOpen: true};
        public event EventHandler<ModbusMessage> OnDataReceived;
        public event EventHandler OnSessionClosed;

        public void Send(ModbusMessage message)
        {
            _writeQueue.Enqueue(message);
            _writeLock.Set();
        }


        public void Start(string name, int baudRate)
        {
            if (string.IsNullOrEmpty(name))
                return;
            _serialPort = new SerialPort(name, baudRate);
            try
            {
                _serialPort.Open();
                
                _serialPort.ReadExisting();
            }
            catch
            {
                return;
            }

            _isRunning = true;

            _readThread = new Thread(() =>
            {
                var exceptionCount = 0;
                var tempBuf = new byte[256];

                while (_isRunning)
                    try
                    {
                        var message = new ModbusMessage();
                        //地址
                        var addr = (byte) _serialPort.ReadByte();
                        message.Address = addr;

                        //兼容一个其他协议
                        if (addr == (byte)'<')
                        {

                            var header = Encoding.ASCII.GetString(new byte[]
                                { (byte)_serialPort.ReadByte(), (byte)_serialPort.ReadByte() });

                            if (header.StartsWith("0E"))
                            {
                                var data = header + Encoding.ASCII.GetString(new byte[]
                                    { (byte)_serialPort.ReadByte(), (byte)_serialPort.ReadByte(), (byte)_serialPort.ReadByte() });
                                //出错了
                                message.ErrorCode = byte.Parse(new string(data.Skip(2).Take(3).ToArray()));
                            }
                            else
                            {
                                var len = int.Parse(new string(header.Skip(1).Take(1).ToArray()));
                                _serialPort.ReadByte();
                                _serialPort.ReadByte();
                                var sb = new StringBuilder();
                                while (len > 0)
                                {
                                    sb.Append((char)_serialPort.ReadChar());
                                    len--;
                                }
                                message.Data = BitConverter.GetBytes(int.Parse(sb.ToString()));
                            }

                            if (_serialPort.ReadChar() == '>')
                            {
                                message.Address = 0x02;
                                message.Command = 0x03;
                                message.Extend = true;
                                //解析完毕
                                OnDataReceived?.Invoke(this, message);
                            }
                            continue;
                        }

       

                        //功能码  03读取  06写入
                        var command = (byte) _serialPort.ReadByte();

                        message.Command = command;

                        //测距仪返回错误ID
                        if (message.Command >= 0x80)
                        {
                            message.ErrorCode = (byte)_serialPort.ReadByte();
                        }
                        else
                        {
                            var dataLen = (byte)_serialPort.ReadByte();


                            var readCount = 0;
                            while (_isRunning && readCount < dataLen) tempBuf[readCount++] = (byte)_serialPort.ReadByte();

                            message.Data = tempBuf.Take(readCount).ToArray();
                        }


                        tempBuf[0] = (byte) _serialPort.ReadByte();
                        tempBuf[1] = (byte) _serialPort.ReadByte();

                        var crc = BitConverter.ToInt16(tempBuf, 0);
                        message.Crc = crc;

                        OnDataReceived?.Invoke(this, message);

                        //Debug.WriteLine(message.ToString());
                    }
                    catch
                    {
                        ++exceptionCount;
                        if (exceptionCount > 10)
                            break;
                        OnSessionClosed?.Invoke(this, EventArgs.Empty);
                    }
            });
            _writeThread = new Thread(() =>
            {
                while (_isRunning)
                {
                    while (_writeQueue.Count > 0)
                    {
                        ModbusMessage ds;
                        _writeQueue.TryDequeue(out ds);
                        if (ds != null)
                        {
                            if (ds.Extend)
                            {
                                if (_serialPort.IsOpen)
                                    _serialPort.Write(ds.Data, 0, ds.Data.Length);
                            }
                            else
                            {
                                var data = ds.ToMessage();
                                if (_serialPort.IsOpen)
                                    _serialPort.Write(data, 0, data.Length);
                               
                            }
                            //Console.WriteLine("write -> " + HexToString(ds.Data));
                        }

                        Thread.Sleep(10);
                    }

                    _writeLock.WaitOne(1000);
                }
            });
            _readThread.IsBackground = true;
            _readThread.Start();
            _writeThread.IsBackground = true;
            _writeThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                _writeLock.Set();
                _writeThread?.Join();
                _serialPort?.Close();
                _readThread?.Join();
            }
            catch
            {
            }
        }
    }
}