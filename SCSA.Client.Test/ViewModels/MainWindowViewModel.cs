﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SCSA.Models;

namespace SCSA.Client.Test.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ConcurrentDictionary<ParameterType, Parameter> _parameters;
    private readonly ConcurrentDictionary<DeviceStatusType, DeviceStatus> _deviceStatus;
    private CancellationTokenSource _cts;

    private Task _dataUploadTask;
    private string _log;

    private Task _recvTask;

    private Socket _tcpClient;

    // IQ 文件路径，可根据需要修改或通过界面绑定
    private string IFilePath;
    private string QFilePath;

    private int _expectedFwSize;
    private int _receivedFwBytes;

    private Timer _upgradeReqTimer;
    private volatile bool _firmwareInfoReceived;

    public MainWindowViewModel()
    {
        IFilePath = Path.Combine(AppContext.BaseDirectory, "I1-1280Hz-1.5米反光膜-新激光器-0.4mms.txt");
        QFilePath = Path.Combine(AppContext.BaseDirectory, "q1-1280Hz-1.5米反光膜-新激光器-0.4mms.txt");
        //var len =BitConverter.ToInt32(new byte[] { 0x06, 0x20, 0x03, 0x00 }.Reverse().ToArray());

        //var netPackage = new PipelineNetDataPackage();
        //netPackage.DeviceCommand = DeviceCommand.ReplyStartCollection;
        //netPackage.CmdId = 0;
        //netPackage.Data = [];
        //netPackage.DataLen = 0;

        //var bytes = netPackage.GetBytes();

        //var str = bytes.Select(b => b.ToString("x2")).Aggregate((p, n) => p + " " + n);

        //Debug.WriteLine(str);

        //var buffer = HexStringToByteArray("53435a4e0001000000000000");

        //uint crc32 = System.IO.Hashing.Crc32.HashToUInt32(buffer);

        //var idx = 0;
        //buffer[idx++] = (byte)(crc32 & 0xFF);
        //buffer[idx++] = (byte)((crc32 >> 8) & 0xFF);
        //buffer[idx++] = (byte)((crc32 >> 16) & 0xFF);
        //buffer[idx++] = (byte)((crc32 >> 24) & 0xFF);

        ConnectToServerCommand = new RelayCommand(ExecuteConnectToServer, () => true);

        _parameters = new ConcurrentDictionary<ParameterType, Parameter>();

        foreach (var parameterType in Enum.GetValues<ParameterType>())
        {
            var t = Parameter.GetParameterType(parameterType);
            var v = Activator.CreateInstance(t);
            var p = new Parameter
            {
                Value = v,
                Address = parameterType,
                Length = Parameter.GetParameterLength(parameterType)
            };
            _parameters.TryAdd(parameterType, p);
        }


        _deviceStatus = new ConcurrentDictionary<DeviceStatusType, DeviceStatus>();
        foreach (var deviceStatusType in Enum.GetValues<DeviceStatusType>())
        {
            var t = DeviceStatus.GetDeviceStatusType(deviceStatusType);
            var v = Activator.CreateInstance(t);
            var p = new DeviceStatus()
            {
                Value = v,
                Address = deviceStatusType,
            };
            _deviceStatus.TryAdd(deviceStatusType, p);
        }
    }

    public string IpAddress { set; get; } = "192.66.66.28";
    public int Port { set; get; } = 9123;

    public RelayCommand ConnectToServerCommand { set; get; }

    public string Log
    {
        set => SetProperty(ref _log, value);
        get => _log;
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

                            short flag = 0;

                            if (_dataUploadTask == null)
                            {
                                _dataUploadTask = new Task(async () =>
                                {
                                    var rand = new Random();
                                    short noiseRange = 1000; // 噪声幅度，越大相关性波动越明显

                                    var p = _parameters.First(pp => pp.Key == ParameterType.TriggerSampleType);
                                    if ((byte)p.Value.Value == (byte)TriggerType.DebugTrigger)
                                        await Task.Delay(TimeSpan.FromSeconds(5));

                                    // 若需要从文件读取 IQ 数据
                                    var iSamples = new List<short>();
                                    var qSamples = new List<short>();

                                    if (File.Exists(IFilePath) && File.Exists(QFilePath))
                                    {
                                        // 跳过首行标题, 解析第二列数值
                                        foreach (var line in File.ReadLines(IFilePath).Skip(1))
                                        {
                                            var parts = line.Split('\t');
                                            if (parts.Length >= 2 && double.TryParse(parts[1], out var v))
                                                iSamples.Add((short)(v * 1000)); // 简单放大
                                        }

                                        foreach (var line in File.ReadLines(QFilePath).Skip(1))
                                        {
                                            var parts = line.Split('\t');
                                            if (parts.Length >= 2 && double.TryParse(parts[1], out var v))
                                                qSamples.Add((short)(v * 1000));
                                        }
                                    }

                                    if (iSamples.Count == 0 || qSamples.Count == 0)
                                    {
                                        // 回退生成信号
                                        iSamples.AddRange(Enumerable.Repeat<short>(0, 1));
                                        qSamples.AddRange(Enumerable.Repeat<short>(0, 1));
                                    }

                                    int fileCursor = 0;

                                    double sampleRate = 20000000;
                                    double frequency = 160000;
                                    var buffer = new byte[10240];
                                    double time = 0;
                                    var timeIncrement = 1.0 / sampleRate;
                                    var stopwatch = new Stopwatch();
                                    stopwatch.Start();

                                    // 计算需要存储的总点数
                                    int totalPoints;
                                    if ((byte)p.Value.Value == (byte)TriggerType.DebugTrigger)
                                    {
                                        // DebugTrigger模式下固定存储64M/4点
                                        totalPoints = 64 * 1024 * 1024 / 4;
                                    }
                                    else
                                    {
                                        // 其他模式下根据采样率和时间计算点数
                                        // 移除硬编码的5秒限制，改为更大的值以支持长时间录制
                                        // 使用long来避免整数溢出，然后安全地转换为int
                                        var longTotalPoints = (long)(sampleRate * 600); // 支持60秒录制
                                        totalPoints = longTotalPoints > int.MaxValue
                                            ? int.MaxValue
                                            : (int)longTotalPoints;
                                    }

                                    var currentPoints = 0;

                                    try
                                    {
                                        while (!_cts.IsCancellationRequested)
                                        {
                                            // 检查是否达到总点数
                                            if (currentPoints >= totalPoints) break;

                                            var returnNetPackage = new NetDataPackage();
                                            returnNetPackage.DeviceCommand = DeviceCommand.ReplyUploadData;
                                            returnNetPackage.Flag = flag++;
                                            if (flag > 2048)
                                                flag = 0;
                                            var list = new List<byte>();
                                            list.AddRange(BitConverter.GetBytes(
                                                Convert.ToInt32(_parameters[ParameterType.UploadDataType].Value))
                                            );

                                            // 计算本次需要发送的点数
                                            int pointsToSend;
                                            if ((byte)p.Value.Value == (byte)TriggerType.DebugTrigger)
                                                // 在调试触发模式下，确保不会超过总点数
                                                pointsToSend = Math.Min(buffer.Length, totalPoints - currentPoints);
                                            else
                                                pointsToSend = buffer.Length;

                                            list.AddRange(BitConverter.GetBytes(pointsToSend));

                                            // 生成数据
                                            var uploadType =
                                                (DataChannelType)Convert.ToByte(
                                                    _parameters[ParameterType.UploadDataType].Value);

                                            if (uploadType == DataChannelType.ISignalAndQSignal)
                                            {
                                                // 从文件缓冲读取 IQ
                                                for (int i = 0; i < pointsToSend; i++)
                                                {
                                                    var iVal = iSamples[fileCursor % iSamples.Count];
                                                    var qVal = qSamples[fileCursor % qSamples.Count];
                                                    fileCursor++;
                                                    list.AddRange(BitConverter.GetBytes(iVal));
                                                    list.AddRange(BitConverter.GetBytes(qVal));
                                                }
                                            }
                                            else
                                            {
                                                // 单通道 float 信号
                                                for (var i = 0; i < pointsToSend; i++)
                                                {
                                                    var sample = Math.Sin(2 * Math.PI * frequency * time) * 127 + 128;
                                                    buffer[i] = (byte)sample;
                                                    time += timeIncrement;
                                                    list.AddRange(BitConverter.GetBytes((float)buffer[i]));
                                                }
                                            }

                                            currentPoints += pointsToSend;

                                            returnNetPackage.Data = list.ToArray();

                                            // 发送数据
                                            Send(returnNetPackage);
                                            // 精确时间控制
                                            var elapsed = stopwatch.Elapsed;
                                            var targetTime = TimeSpan.FromSeconds(pointsToSend / sampleRate);
                                            if (elapsed < targetTime)
                                            {
                                                //await Task.Delay(targetTime - elapsed, _cts.Token);
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
                            returnNetPackage.Flag = netPackage.Flag;
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
                            returnNetPackage.Flag = netPackage.Flag;
                            returnNetPackage.Data = BitConverter.GetBytes((short)0);
                            Send(returnNetPackage);
                        }

                            break;
                        case DeviceCommand.RequestSetParameters:
                        {
                            var returnParameters = new List<Parameter>();
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var len = reader.ReadInt32();
                            for (var i = 0; i < len; i++)
                            {
                                var parameter = new Parameter();
                                parameter.Address = (ParameterType)reader.ReadInt32();
                                parameter.Length = reader.ReadInt32();
                                parameter.RawValue = reader.ReadBytes(parameter.Length);
                                parameter.Value = parameter.ToParameterData();

                                _parameters.AddOrUpdate(parameter.Address, parameter, (key, oldValue) => parameter);

                                returnParameters.Add(parameter);
                            }

                            var returnNetPackage = new NetDataPackage();
                            returnNetPackage.DeviceCommand = DeviceCommand.ReplySetParameters;
                            returnNetPackage.Flag = netPackage.Flag;
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
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var len = reader.ReadInt32();
                            for (var i = 0; i < len; i++) addressList.Add((ParameterType)reader.ReadInt32());

                            foreach (var parameterType in addressList)
                            {
                                var p = new Parameter
                                {
                                    Address = parameterType,
                                    Length = Parameter.GetParameterLength(parameterType)
                                };
                                if (_parameters.ContainsKey(parameterType))
                                {
                                    p.Value = _parameters[parameterType].Value;
                                }

                                returnParameters.Add(p);
                            }

                            var returnNetPackage = new NetDataPackage();
                            returnNetPackage.DeviceCommand = DeviceCommand.ReplyReadParameters;
                            returnNetPackage.Flag = netPackage.Flag;
                            var list = new List<byte>();
                            list.AddRange(BitConverter.GetBytes((short)0));
                            list.AddRange(BitConverter.GetBytes(returnParameters.Count));
                            foreach (var returnParameter in returnParameters)
                            {
                                var bytes = BitConverter.GetBytes((uint)returnParameter.Address);
                                list.AddRange(bytes
                                );
                                list.AddRange(BitConverter.GetBytes(returnParameter.Length));
                                list.AddRange(returnParameter.GetParameterData());
                            }

                            returnNetPackage.Data = list.ToArray();
                            Send(returnNetPackage);
                        }

                            break;
                        case DeviceCommand.RequestGetParameterIds:
                        {

                            var returnNetPackage = new NetDataPackage();
                            returnNetPackage.DeviceCommand = DeviceCommand.ReplyGetParameterIds;
                            returnNetPackage.Flag = netPackage.Flag;
                            var list = new List<byte>();
                            list.AddRange(BitConverter.GetBytes((short)0));
                            list.AddRange(BitConverter.GetBytes(_parameters.Count));
                            foreach (var returnParameter in _parameters.Values)
                            {
                                var bytes = BitConverter.GetBytes((uint)returnParameter.Address);
                                list.AddRange(bytes);
                            }

                            returnNetPackage.Data = list.ToArray();
                            Send(returnNetPackage);
                        }
                            break;
                        case DeviceCommand.RequestGetDeviceStatus:
                        {

                            var addressDeviceStatusTypes = new List<DeviceStatusType>();
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var len = reader.ReadInt32();
                            for (int i = 0; i < len; i++)
                            {

                                addressDeviceStatusTypes.Add((DeviceStatusType)reader.ReadInt32());
                            }

                            var returnDeviceStatus = new List<DeviceStatus>();

                            foreach (var deviceStatusType in addressDeviceStatusTypes)
                            {
                                var p = new DeviceStatus()
                                {
                                    Address = deviceStatusType,
                                };
                                if (_deviceStatus.ContainsKey(deviceStatusType))
                                {
                                    p.Value = _deviceStatus[deviceStatusType].Value;
                                }

                                returnDeviceStatus.Add(p);
                            }

                            var returnNetPackage = new NetDataPackage();
                            returnNetPackage.DeviceCommand = DeviceCommand.ReplyGetDeviceStatus;
                            returnNetPackage.Flag = netPackage.Flag;
                            var list = new List<byte>();
                            list.AddRange(BitConverter.GetBytes((short)0));
                            list.AddRange(BitConverter.GetBytes(returnDeviceStatus.Count));
                            foreach (var deviceStatus in returnDeviceStatus)
                            {
                                list.AddRange(BitConverter.GetBytes((int)deviceStatus.Address));
                                list.AddRange(deviceStatus.GetParameterData());
                            }

                            returnNetPackage.Data = list.ToArray();
                            Send(returnNetPackage);
                        }
                            break;
                        case DeviceCommand.RequestStartFirmwareUpgrade:
                        {
                            // 0xF8 无数据负载 -> 回复 0xF9
                            var reply = new NetDataPackage
                            {
                                Flag = netPackage.Flag,
                                DeviceCommand = DeviceCommand.ReplyStartFirmwareUpgrade,
                                Data = Array.Empty<byte>()
                            };
                            Send(reply);

                            // 模拟设备重启：关闭当前连接，稍后重连并开始发送 0xFA
                            Task.Run(async () =>
                            {
                                // 关闭
                                CloseConnection();

                                // 模拟重启耗时
                                await Task.Delay(1500);

                                // 重新连接到 PC
                                ExecuteConnectToServer();

                                // 开始定时发送 0xFA
                                StartUpgradeRequestTimer();
                            });
                        }

                            break;
                        case DeviceCommand.RequestSendFirmwareInfo:
                        {
                            // 解析: 加密CRC(4) + size(4)
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var encCrc = reader.ReadBytes(4);
                            var fwSize = reader.ReadInt32();

                            _expectedFwSize = fwSize;
                            _receivedFwBytes = 0;
                            _firmwareInfoReceived = true;
                            _upgradeReqTimer?.Dispose();

                            var reply = new NetDataPackage
                            {
                                Flag = netPackage.Flag,
                                DeviceCommand = DeviceCommand.ReplySendFirmwareInfo,
                                Data = BitConverter.GetBytes((short)0) // 成功
                            };
                            Send(reply);
                        }

                            break;
                        case DeviceCommand.RequestTransferFirmwareUpgrade:
                        {
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var packetId = reader.ReadInt32();
                            var len = reader.ReadInt32();
                            var data = reader.ReadBytes(len);

                            _receivedFwBytes += len;

                            var list = new List<byte>();
                            list.AddRange(BitConverter.GetBytes(packetId));
                            list.AddRange(BitConverter.GetBytes(len));
                            list.AddRange(BitConverter.GetBytes((short)0));

                            var reply = new NetDataPackage
                            {
                                Flag = netPackage.Flag,
                                DeviceCommand = DeviceCommand.ReplyTransferFirmwareUpgrade,
                                Data = list.ToArray()
                            };
                            Send(reply);

                            // 如果已接收完整固件，发送升级结果(0xFF)
                            if (_receivedFwBytes >= _expectedFwSize)
                            {
                                var resultPkg = new NetDataPackage
                                {
                                    Flag = 0,
                                    DeviceCommand = DeviceCommand.ReplyFirmwareUpgradeResult,
                                    Data = BitConverter.GetBytes((short)0) // 成功
                                };
                                Send(resultPkg);
                            }
                        }

                            break;

                        case DeviceCommand.RequestStartPulseOutput:
                        {
                            // 解析脉冲输出参数 (12字节: param1, param2, param3 各4字节)
                            var reader = new BinaryReader(new MemoryStream(netPackage.Data));
                            var param1 = reader.ReadInt32();
                            var param2 = reader.ReadInt32();
                            var param3 = reader.ReadInt32();

                            AppendLog($"开始脉冲输出 - 参数1: {param1}, 参数2: {param2}, 参数3: {param3}");

                            // 模拟脉冲输出启动成功
                            var reply = new NetDataPackage
                            {
                                Flag = netPackage.Flag,
                                DeviceCommand = DeviceCommand.ReplyStartPulseOutput,
                                Data = BitConverter.GetBytes((short)0) // 成功
                            };
                            Send(reply);
                        }
                            break;

                        case DeviceCommand.RequestStopPulseOutput:
                        {
                            AppendLog("停止脉冲输出");

                            // 模拟脉冲输出停止成功
                            var reply = new NetDataPackage
                            {
                                Flag = netPackage.Flag,
                                DeviceCommand = DeviceCommand.ReplyStopPulseOutput,
                                Data = BitConverter.GetBytes((short)0) // 成功
                            };
                            Send(reply);
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
        var length = hex.Length;
        var bytes = new byte[length / 2];

        for (var i = 0; i < length; i += 2) bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

        return bytes;
    }

    private void CloseConnection()
    {
        try
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                _tcpClient.Shutdown(SocketShutdown.Both);
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            try
            {
                _tcpClient?.Close();
            }
            catch { }
        }
    }

    private void StartUpgradeRequestTimer()
    {
        _upgradeReqTimer?.Dispose();
        _firmwareInfoReceived = false;

        _upgradeReqTimer = new Timer(_ =>
        {
            if (_firmwareInfoReceived)
            {
                _upgradeReqTimer?.Dispose();
                return;
            }

            var req = new NetDataPackage
            {
                Flag = 0,
                DeviceCommand = DeviceCommand.DeviceRequestFirmwareUpgrade,
                Data = Array.Empty<byte>()
            };
            Send(req);
        }, null, 0, 1000);
    }
}