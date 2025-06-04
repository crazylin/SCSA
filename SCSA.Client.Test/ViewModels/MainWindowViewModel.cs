using CommunityToolkit.Mvvm.Input;
using System.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HarfBuzzSharp;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using SCSA.Models;


namespace SCSA.Client.Test.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string IpAddress { set; get; } = "192.66.66.28";
        public int Port { set; get; } = 9123;

        private Socket _tcpClient;
        private string _log;

        public RelayCommand ConnectToServerCommand { set; get; }

        public string Log
        {
            set => SetProperty(ref _log, value);
            get => _log;
        }

        private Task _recvTask;

        private Task _dataUploadTask;
        private CancellationTokenSource _cts;
        private ConcurrentDictionary<ParameterType, Parameter> _parameters;

        public MainWindowViewModel()
        {

            //var len =BitConverter.ToInt32(new byte[] { 0x06, 0x20, 0x03, 0x00 }.Reverse().ToArray());
            ConnectToServerCommand = new RelayCommand(ExecuteConnectToServer, () => true);

            _parameters = new ConcurrentDictionary<ParameterType, Parameter>();
           
            foreach (var parameterType in Enum.GetValues<ParameterType>())
            {
                var t = Parameter.GetParameterType(parameterType);
                var v = Activator.CreateInstance(t);
                var p = new Parameter()
                {
                    Value = v,
                    Address = parameterType,
                    Length = Parameter.GetParameterLength(parameterType)
                };
                _parameters.TryAdd(parameterType, p);

            }

        }

        public void ExecuteConnectToServer()
        {
            AppendLog("开始连接");
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                if (_recvTask != null)
                    _recvTask?.Wait();
            }

            if (!IPAddress.TryParse(IpAddress, out var ip))
            {
                AppendLog("IP地址或者端口错误");
                return;
            }

            ;
            try
            {
                _tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _tcpClient.Connect(ip, Port);
            }
            catch (Exception e)
            {
                AppendLog($"连接失败: {e.Message}");
                _tcpClient.Close();
                _tcpClient.Dispose();
                return;
            }

            //var str =
            //    "53435a4e000400000620030000080000ddfe58ffdbfe59ffdbfe5bffddfe55ffdafe57ffdbfe57ffdefe58ffdefe58ffdffe59ffdcfe58ffdbfe57ffdbfe5affdbfe5affddfe57ffe0fe57ffdffe57ffdcfe58ffdafe5cffdbfe5cffdcfe5bffdbfe5affdefe57ffd8fe59ffdafe56ffd8fe59ffddfe57ffdafe5dffdcfe57ffddfe54ffdbfe55ffdafe5affdcfe55ffdafe59ffdafe5affdbfe5affdbfe58ffdcfe59ffdcfe59ffdafe58ffdcfe57ffdbfe58ffddfe59ffdcfe58ffd9fe59ffdbfe5affdcfe58ffdcfe59ffddfe59ffdcfe59ffddfe58ffe0fe56ffd6fe56ffddfe57ffdcfe56ffddfe5effdafe5bffdafe59ffddfe5cffdcfe58ffddfe58ffd9fe52ffddfe58ffddfe5bffdafe5cffdefe58ffddfe57ffddfe57ffdefe5bffdefe5affdafe58ffdafe5affd7fe59ffdbfe58ffdbfe59ffdffe57ffdbfe59ffdafe54ffd7fe56ffddfe58ffdbfe57ffd9fe54ffdbfe58ffdafe56ffddfe57ffdcfe59ffd9fe5bffd9fe59ffd8fe56ffdcfe5bffdbfe59ffddfe58ffe0fe5affdafe57ffdbfe56ffdcfe57ffd9fe58ffdcfe5cffdafe57ffdefe59ffddfe57ffdefe59ffdffe5bffd8fe58ffdcfe55ffddfe58ffd8fe57ffdcfe57ffdafe56ffddfe58ffdefe5bffddfe59ffdbfe58ffdcfe57ffd8fe59ffdffe5affdafe58ffddfe56ffddfe58ffdbfe55ffdafe5affdcfe5affdffe58ffdffe56ffdbfe5affdbfe5affdcfe5bffddfe57ffd9fe59ffdefe59ffdafe59ffddfe5bffdcfe55ffdbfe5bffe0fe55ffdbfe53ffdcfe5affdbfe58ffddfe59ffdbfe5bffddfe58ffd9fe59ffe0fe57ffdefe57ffddfe58ffddfe5affdffe5bffdcfe56ffdffe54ffdbfe58ffdcfe58ffddfe55ffdbfe5bffddfe59ffdefe5bffdbfe5bffdafe56ffdefe58ffdcfe57ffdcfe5dffdafe57ffe0fe57ffddfe5affdcfe5affddfe58ffdefe58ffdcfe5affdefe58ffdbfe59ffdbfe58ffd8fe59ffdbfe58ffe2fe59ffdcfe5affddfe59ffdafe54ffdbfe57ffd8fe56ffdcfe57ffddfe5affddfe5affdafe58ffdafe5affdbfe59ffdcfe5affdcfe5affdffe57ffdafe57ffdefe5affdefe5affdbfe58ffdffe59ffdbfe5bffdffe58ffdefe57ffdbfe59ffd9fe59ffdafe57ffdcfe59ffdafe57ffddfe56ffd9fe58ffd7fe58ffdcfe58ffdffe59ffd9fe56ffdbfe59ffdafe58ffdbfe5affddfe58ffdcfe56ffddfe57ffddfe5affdcfe57ffe0fe57ffdefe58ffd8fe59ffd9fe5affd9fe58ffd9fe5affddfe5bffdbfe58ffd9fe59ffdefe5effdbfe58ffd9fe5bffdbfe55ffdbfe56ffdffe56ffdbfe56ffdcfe56ffd9fe5dffd9fe58ffdafe58ffdafe59ffdcfe5affddfe54ffdafe56ffdbfe59ffdbfe5affd9fe5bffdcfe57ffddfe54ffdbfe57ffdbfe5affdefe5affe0fe59ffdffe58ffdefe5dffdbfe55ffd9fe59ffdbfe57ffdefe58ffd9fe54ffdefe5affddfe55ffddfe57ffd9fe5dffdefe5fffd9fe5affddfe54ffdafe59ffdbfe58ffd9fe59ffdafe56ffd9fe57ffdbfe5dffddfe55ffdcfe58ffd9fe59ffd8fe59ffe0fe59ffdffe5affe0fe54ffddfe56ffd5fe59ffdcfe58ffdafe55ffe1fe59ffdafe58ffd8fe59ffddfe59ffdbfe5affdcfe58ffd8fe5bffdcfe5bffd9fe5affe0fe57ffdcfe5affdcfe56ffe0fe5affd9fe5bffdafe59ffddfe58ffdbfe54ffd9fe59ffdafe56ffdbfe59ffdcfe58ffdbfe57ffe0fe5affdcfe5affddfe58ffdcfe58ffdafe57ffdcfe59ffdffe5bffdcfe58ffdcfe57ffdffe58ffddfe56ffddfe59ffdefe59ffdbfe54ffddfe57ffdbfe5dffdefe55ffdcfe58ffddfe57ffdefe58ffd9fe54ffdbfe56ffddfe55ffdcfe5affd9fe59ffdafe5affdcfe5affdcfe59ffdafe58ffdffe58ffdefe5affddfe5bffdbfe58ffdbfe59ffddfe57ffdbfe57ffdcfe56ffdffe56ffdafe59ffdafe53ffdcfe56ffd8fe58ffdafe56ffdafe56ffdcfe55ffdcfe59ffddfe53ffdafe58ffe0fe57ffe0fe59ffdefe57ffddfe59ffddfe57ffdefe57ffdcfe59ffdafe5affdcfe57ffd8fe5affdbfe";
            //var bytes = HexStringToByteArray(str);
            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        _tcpClient.Send(bytes);
            //        break;
            //        Thread.Sleep(0);
            //    }
            //});
            //return;
            AppendLog("连接成功");
            _recvTask = Task.Factory.StartNew(() =>
            {

                var stream = new BinaryReader(new NetworkStream(_tcpClient, true));

                while (_tcpClient.Connected)
                {
                    try
                    {
                        var netPackage = new NetDataPackage();
                        netPackage.Read(stream);

                        AppendLog($"收到命令 {netPackage.DeviceCommand}");
                        switch (netPackage.DeviceCommand)
                        {
                            case DeviceCommand.RequestStartCollection:
                            {
                                    _cts = new CancellationTokenSource();


                                    if (_dataUploadTask == null)
                                    {
                                        _dataUploadTask = new Task(async () =>
                                        {
                                            double sampleRate = 20000000;
                                            double frequency = 160000;
                                            var buffer = new byte[10240];
                                            double time = 0;
                                            double timeIncrement = 1.0 / sampleRate;
                                            var stopwatch = new Stopwatch();
                                            stopwatch.Start();

                                            try
                                            {
                                                while (!_cts.IsCancellationRequested)
                                                {
                                                    var returnNetPackage = new NetDataPackage();
                                                    returnNetPackage.DeviceCommand = DeviceCommand.ReplyUploadData;
                                                    var list = new List<byte>();
                                                    list.AddRange(BitConverter.GetBytes((int)Convert.ToInt32(_parameters[ParameterType.UploadDataType].Value))
                                                       );
                                                    list.AddRange(BitConverter.GetBytes(buffer.Length));

                                                    // 生成正弦波数据
                                                    for (int i = 0; i < buffer.Length; i++)
                                                    {
                                                        double sample = Math.Sin(2 * Math.PI * frequency * time) * 127 +
                                                                        128;
                                                        buffer[i] = (byte)sample;
                                                        time += timeIncrement;
                                                        list.AddRange(
                                                            BitConverter.GetBytes((float)buffer[i]));
                                                    }

                                                    returnNetPackage.Data = list.ToArray();

                                                    // 发送数据
                                                    Send(returnNetPackage);
                                                    // 精确时间控制
                                                    var elapsed = stopwatch.Elapsed;
                                                    var targetTime = TimeSpan.FromSeconds(buffer.Length / sampleRate);
                                                    if (elapsed < targetTime)
                                                    {
                                                        await Task.Delay(targetTime - elapsed, _cts.Token);
                                                    }

                                                    stopwatch.Restart();
                                                }
                                            }
                                            catch (Exception e)
                                            {

                                            }

                                        });
                                        _dataUploadTask.Start();
                                    }

                                var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplyStartCollection;
                                returnNetPackage.Data = BitConverter.GetBytes((short)0);

                                // 发送数据
                                Send(returnNetPackage);

                            }
                                break;

                            case DeviceCommand.RequestStopCollection:
                            {
                                    _cts.Cancel();
                                    _dataUploadTask?.Wait();
                                    _dataUploadTask = null;

                                    var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplyStopCollection;
                                returnNetPackage.Data = BitConverter.GetBytes((short)0);
                                Send(returnNetPackage);
                            }

                                break;
                            case DeviceCommand.RequestSetParameters:
                            {
                                var returnParameters = new List<Parameter>();
                                BinaryReader reader = new BinaryReader(new MemoryStream(netPackage.Data));
                                var len = reader.ReadInt32();
                                for (int i = 0; i < len; i++)
                                {
                                    var parameter = new Parameter();
                                    parameter.Address = (ParameterType)reader.ReadInt32();
                                    parameter.Length = reader.ReadInt32();
                                    parameter.RawValue = reader.ReadBytes((int)parameter.Length);
                                    parameter.Value = parameter.ToParameterData();

                                    _parameters.AddOrUpdate(parameter.Address, parameter, (key, oldValue) => parameter);

                                    returnParameters.Add(parameter);
                                }

                                var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplySetParameters;
                                //var list = new List<byte>();
                                //list.AddRange(BitConverter.GetBytes(returnParameters.Count));
                                //foreach (var returnParameter in returnParameters)
                                //{
                                //    list.AddRange(
                                //        BitConverter.GetBytes((int)returnParameter.Address));
                                //    list.AddRange(BitConverter.GetBytes((short)0));
                                //}

                                returnNetPackage.Data = BitConverter.GetBytes((short)0);
                                Send(returnNetPackage);

                            }
                                break;

                            case DeviceCommand.RequestReadParameters:
                            {
                                var returnParameters = new List<Parameter>();

                                var addressList = new List<ParameterType>();
                                BinaryReader reader = new BinaryReader(new MemoryStream(netPackage.Data));
                                var len = reader.ReadInt32();
                                for (int i = 0; i < len; i++)
                                {
                                    addressList.Add((ParameterType)reader.ReadInt32());
                                }

                                foreach (var parameterType in addressList)
                                {
                                    var p = new Parameter() { Address = parameterType, GetResult = false, Length = Parameter.GetParameterLength(parameterType) };
                                    if (_parameters.ContainsKey(parameterType))
                                    {
                                        p.Value = _parameters[parameterType].Value;
                                        p.GetResult = true;
                                    }

                                    returnParameters.Add(p);
                                }

                                var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplyReadParameters;
                                var list = new List<byte>();
                                list.AddRange(BitConverter.GetBytes(returnParameters.Count));
                                foreach (var returnParameter in returnParameters)
                                {
                                    list.AddRange(
                                        BitConverter.GetBytes((int)returnParameter.Address));
                                    list.AddRange(
                                        BitConverter.GetBytes(returnParameter.GetResult
                                            ? (short)0
                                            : (short)1));
                                    list.AddRange(BitConverter.GetBytes(returnParameter.Length));
                                    list.AddRange(returnParameter.GetParameterData());
                                }

                                returnNetPackage.Data = list.ToArray();
                                Send(returnNetPackage);
                            }

                                break;

                            case DeviceCommand.RequestStartFirmwareUpgrade:
                            {
                                var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                                var type = reader.ReadInt32();
                                var size = reader.ReadInt32();

                                var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplyStartFirmwareUpgrade;
                                returnNetPackage.Data = BitConverter.GetBytes((int)(0))
                                    .Concat(BitConverter.GetBytes((short)0)).ToArray();
                                Send(returnNetPackage);
                            }

                                break;
                            case DeviceCommand.RequestTransferFirmwareUpgrade:
                            {
                                var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                                var type = reader.ReadInt32();
                                var packetId = reader.ReadInt32();
                                var len = reader.ReadInt32();
                                var data = reader.ReadBytes(len);
                                Debug.WriteLine("Update PacketId: " + packetId);
                                var returnNetPackage = new NetDataPackage();
                                returnNetPackage.DeviceCommand = DeviceCommand.ReplyTransferFirmwareUpgrade;
                                var list = new List<byte>();

                                list.AddRange(BitConverter.GetBytes(type));
                                list.AddRange(BitConverter.GetBytes(packetId));
                                list.AddRange(BitConverter.GetBytes(len));
                                list.AddRange(BitConverter.GetBytes((short)0));
                                returnNetPackage.Data = list.ToArray();
                                Send(returnNetPackage);
                            }
                                break;

                        }
                        ////返回数据
                        //var data = Encoding.UTF8.GetString(netPackage.Data);
                        //var command = netPackage.DeviceCommand;
                        //AppendLog(data.Length <= 2
                        //    ? $"收到命令 Command: {command} Data: {((NetErrorCode)int.Parse(data)).ToString()}"
                        //    : $"收到命令 Command: {command} Data: {data}");

                        ////如果是模型数据 取出模型 模型可能会创建多个 这边可以获取多个模型
                        //if (netPackage.DeviceCommand == CommandType.ReplyModelInfo)
                        //{
                        //    var models = JsonConvert.DeserializeObject<List<ModelInfo>>(Encoding.UTF8.GetString(netPackage.Data));

                        //}
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }
            });
        }

        public void Send(NetDataPackage netPackage)
        {
            if (_tcpClient != null)
            {
                var bytes = netPackage.Get();
                AppendLog($"发送命令 {netPackage.DeviceCommand}");
                _tcpClient.Send(bytes);
                //Debug.WriteLine($"Client Send: {bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");

            }

        }

        public void AppendLog(string log)
        {
            var msg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff ->} {log}\n";
            //Log += msg;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
