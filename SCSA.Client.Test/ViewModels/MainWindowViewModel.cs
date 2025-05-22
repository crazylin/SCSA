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
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using System.Security.Cryptography;

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
            ConnectToServerCommand = new RelayCommand(ExecuteConnectToServer, () => true);

            _parameters = new ConcurrentDictionary<ParameterType, Parameter>();

            foreach (var parameterType in Enum.GetValues<ParameterType>())
            {
        
                _parameters.TryAdd(parameterType, new Parameter()
                {
                    Value = (byte)0x00,
                    Address = parameterType,
                    Length = 1
                });

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
            //    "4e5a435300045f017d050000005e010000000000000a4880e100000000aa8a08e8000000002a80288800000000a8a8088a000000000228a82a0000000040a2028800000000a022200000000000982822a0000000008981a00a000000002802080a00000000aa24a2000000000000223a02000000000022000a0000000001eaa2820000000028a238aa0000000001c40880000000008a920700000000008020a1080000000020a10a000000000022020080000000001220008200000000a8860008000000000a0020280000000000c0a42800000000982a2caa0000000083808a29000000003838288a000000000028888200000000de22a8a30000000082282a0000000000882ea202000000000188aaea00000000a62a08aa0000000080842382000000008982222a000000000802a0a8000000008082210200000000a2820a0200000000a2a820a8000000000220a20800000000a228420600000000a800a23a0000000098a28a2800000000222020820000000083228a2300000000aa02a222000000008a02aa2a00000000a88a0006000000008a80322a00000000a228420800000000a8820a2a000000008aaba2a00000000088880a8a00000000c008aa3a00000000aa88882a00000000b8a2a08200000000a0082080000000004880002000000000a28a808800000000802a80220000000022a20a000000000000a8828a00000000a82888a800000000a08228b2000000002068410600000000828a028a000000002820a1220000000082202080000000002022281e000000000ba8c23200000000a288c880000000000a80a88a00000000018a08aa000000006a086d380000000082888008000000002880a82200000000a228ab920000000020a202aa00000000a822882a00000000040a2222000000000808862a000000002802822c00000000800280a00000000080880282000000000a828aa000000000a80a2a2800000000aa2aa0aa00000000880a08220000000080809028000000000282a02800000000008a388800000000408810a200000000a80a208000000000a888aa02000000000aaaa0a800000000822808800000000082208a2800000000a880a022000000008aa88882000000000800482a0000000008288808000000000000aa000000000020aa9a22000000002a203080000000000222022a00000000080aa22a000000008822308a0000000022aaa888000000000a2a08020000000082822222000000000a8b8b8000000000a24002c800000000080820a000000000a8a022000000000088832aa0000000008c00b0000000000029008082000000008aa8a8c800000000aa22a05a000000002a228802000000008e0088a0000000000a8a202a00000000a28022800000000020a028a0000000008aa208280000000008000020000000000280283a000000002080a8000000000022888a260000000002a022140000000028a82680000000002082ab0000000000100802b000000000a222da0800000000288a42a200000000a8408a88000000000882808a0000000069288a000000000002a0080200000000a2a00808000000003aa2a0c800000000880a8602000000000a292b82000000003308220a0000000028ac208e000000001208000a000000008a22222800000000a888288a00000000a080200800000000268ab00a0000000082a80a8a0000000082a8a8a800000000422a0a8a0000000082aa088800000000baa2008000000000a2882ae800000000880a8822000000008a08828a00000000a02aa92800000000828000a2000000000a21022000000000188b8208000000000a82020c00000000000a202a0000000080a82a2000000000a8a2a80200000000aa8880a0000000008008002a0000000018068a8000000000284008a200000000222a082a000000002808888a00000000008480a200000000280208000000000002a23a2a4f0cc91f";
            //var bytes = HexStringToByteArray(str);
            //Task.Factory.StartNew(() =>
            //{
            //    while (true)
            //    {
            //        _tcpClient.Send(bytes);
            //        Thread.Sleep(0);
            //    }
            //});
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
                                                    list.AddRange(_parameters[ParameterType.UploadDataType]
                                                        .GetParameterData());
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
                                    var p = new Parameter() { Address = parameterType, GetResult = false, Length = 1 };
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
