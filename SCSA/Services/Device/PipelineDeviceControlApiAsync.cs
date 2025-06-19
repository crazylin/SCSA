using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Utils;

namespace SCSA.Services.Device;

public class PipelineDeviceControlApiAsync : IDisposable
{
    private readonly DeviceControlSetting _deviceControlSetting;

    private readonly ConcurrentDictionary<DeviceCommand, List<TaskCompletionSource<PipelineNetDataPackage>>>
        _pendingCommands;

    private readonly PipelineTcpClient<PipelineNetDataPackage> _tcpClient;
    private DateTime? _lastDataReceivedTime;
    private short _flag;

    public PipelineDeviceControlApiAsync(PipelineTcpClient<PipelineNetDataPackage> tcpClient)
    {
        _tcpClient = tcpClient;
        tcpClient.DataReceived += TcpClient_DataReceived;
        _pendingCommands =
            new ConcurrentDictionary<DeviceCommand, List<TaskCompletionSource<PipelineNetDataPackage>>>();
    }

    public void Dispose()
    {
        _tcpClient.DataReceived -= TcpClient_DataReceived;
    }

    public event Action<Dictionary<Parameter.DataChannelType, double[,]>> DataReceived;


    private void TcpClient_DataReceived(object? sender, PipelineNetDataPackage e)
    {
        var netDataPackage = e;

        if (netDataPackage.DeviceCommand != DeviceCommand.ReplyUploadData)
        {
            //var hexData = netDataPackage.GetBytes().Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n);
            //Log.Debug(
            //$"CMD: {netDataPackage.DeviceCommand} DataLen {netDataPackage.DataLen} Data {hexData} Crc {BitConverter.GetBytes(netDataPackage.Crc).Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");
        }

        //Log.Debug(
        //    $"CMD: {netDataPackage.DeviceCommand} DataLen {netDataPackage.DataLen} Crc {BitConverter.GetBytes(netDataPackage.Crc).Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");
        var deviceCommand = netDataPackage.DeviceCommand;

        switch (deviceCommand)
        {
            case DeviceCommand.ReplyStartCollection:
                deviceCommand = DeviceCommand.RequestStartCollection;
                break;
            case DeviceCommand.ReplyStopCollection:
                deviceCommand = DeviceCommand.RequestStopCollection;
                break;
            case DeviceCommand.ReplyUploadData:
                //数据上传
                var now = DateTime.Now;
                if (_lastDataReceivedTime.HasValue)
                {
                    var interval = now - _lastDataReceivedTime.Value;
                    Log.Info($"ReplyUploadData time interval: {interval.TotalMilliseconds:F2} ms");
                }
                _lastDataReceivedTime = now;

                var data = ProcessChannelData(netDataPackage.Data);
                DataReceived?.Invoke(data);

                return;
            case DeviceCommand.ReplySetParameters:
                deviceCommand = DeviceCommand.RequestSetParameters;
                break;
            case DeviceCommand.ReplyReadParameters:
                deviceCommand = DeviceCommand.RequestReadParameters;
                break;
            case DeviceCommand.ReplyStartFirmwareUpgrade:
                deviceCommand = DeviceCommand.RequestStartFirmwareUpgrade;
                break;
            case DeviceCommand.ReplyTransferFirmwareUpgrade:
                deviceCommand = DeviceCommand.RequestTransferFirmwareUpgrade;
                break;
        }

        try
        {
            if (_pendingCommands.TryGetValue(deviceCommand, out var tcsList))
                lock (tcsList)
                {
                    foreach (var tcs in tcsList) tcs.TrySetResult(netDataPackage);

                    // 移除该命令的所有 TCS
                    _pendingCommands.TryRemove(netDataPackage.DeviceCommand, out _);
                }
        }
        catch (Exception exception)
        {
            // 处理异常
        }
    }

    public async Task<PipelineNetDataPackage> SendAsync(DeviceCommand command, byte[] data,
        CancellationToken cancellationToken)
    {
        var netPackage = new PipelineNetDataPackage();
        netPackage.DeviceCommand = command;
        netPackage.CmdId = _flag++;
        if (_flag > 2048)
            _flag = 0;
        netPackage.Data = data;
        netPackage.DataLen = data.Length;

        //var bytes = netPackage.GetBytes();

        //var hexData = bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n);
        //Log.Debug(
        //    $"CMD: {netPackage.DeviceCommand} DataLen {netPackage.DataLen} Data {hexData} Crc {BitConverter.GetBytes(netPackage.Crc).Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");

        var tcs = new TaskCompletionSource<PipelineNetDataPackage>();

        var tcsList = _pendingCommands.GetOrAdd(netPackage.DeviceCommand,
            _ => new List<TaskCompletionSource<PipelineNetDataPackage>>());
        lock (tcsList)
        {
            tcsList.Add(tcs);
        }

        if (!await _tcpClient.SendAsync(netPackage))
        {
            lock (tcsList)
            {
                tcsList.Remove(tcs);
                if (tcsList.Count == 0)
                    _pendingCommands.TryRemove(netPackage.DeviceCommand, out _);
            }

            return null;
        }

        try
        {
            await using (cancellationToken.Register(() =>
                         {
                             tcs.TrySetCanceled();
                             lock (tcsList)
                             {
                                 tcsList.Remove(tcs);
                                 if (tcsList.Count == 0)
                                     _pendingCommands.TryRemove(netPackage.DeviceCommand, out _);
                             }
                         }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException e)
        {
            lock (tcsList)
            {
                tcsList.Remove(tcs);
                if (tcsList.Count == 0)
                    _pendingCommands.TryRemove(netPackage.DeviceCommand, out _);
            }

            return null;
        }
    }

    public async Task<bool> Start(CancellationToken cancellationToken)
    {
        var netDataPackage = await SendAsync(DeviceCommand.RequestStartCollection, [], cancellationToken);
        if (netDataPackage == null)
            return false;

        return BitConverter.ToInt16(netDataPackage.Data) == 0;
    }

    public async Task<bool> Stop(CancellationToken cancellationToken)
    {
        var netDataPackage = await SendAsync(DeviceCommand.RequestStopCollection, [], cancellationToken);
        if (netDataPackage == null)
            return false;
        return BitConverter.ToInt16(netDataPackage.Data) == 0;
    }

    public async Task<(bool success, List<Parameter> result)> ReadParameters(List<Parameter> parameters,
        CancellationToken cancellationToken)
    {
        var data = Parameter.Get_GetParametersData(parameters.ToList());

        var netDataPackage = await SendAsync(DeviceCommand.RequestReadParameters, data, cancellationToken);
        if (netDataPackage == null)
            return (false, parameters);

        var resultParameters = Parameter.Get_GetParametersResult(netDataPackage.Data);
        var returnParameters = new List<Parameter>();
        foreach (var resultParameter in resultParameters)
        {
            var p = parameters.FirstOrDefault(p => p.Address == resultParameter.Address);
            if (p != null && resultParameter.GetResult)
            {
                p.Value = resultParameter.ToParameterData();
                returnParameters.Add(p);
            }
        }

        return (true, returnParameters);
    }

    public async Task<bool> SetParameters(List<Parameter> parameters,
        CancellationToken cancellationToken)
    {
        var data = Parameter.Get_SetParametersData(parameters);

        var netDataPackage = await SendAsync(DeviceCommand.RequestSetParameters, data, cancellationToken);
        if (netDataPackage == null)
            return false;

        return BitConverter.ToInt16(netDataPackage.Data) == 0;
    }

    public async Task<bool> FirmwareUpgradeStart(int size, CancellationToken cancellationToken)
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(0)); //0x00000000 表示开始传输
        data.AddRange(BitConverter.GetBytes(size));

        var netDataPackage = await SendAsync(DeviceCommand.RequestStartFirmwareUpgrade, data.ToArray(),
            cancellationToken);
        if (netDataPackage == null)
            return false;

        return BitConverter.ToInt16(netDataPackage.Data.Skip(4).Take(2).ToArray()) == 0;
    }

    public async Task<bool> FirmwareUpgradeTransfer(int packageId, byte[] firmwareData,
        CancellationToken cancellationToken)
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(1)); //表示传输的是固件包
        data.AddRange(BitConverter.GetBytes(packageId));
        data.AddRange(BitConverter.GetBytes(firmwareData.Length));
        data.AddRange(firmwareData);

        var netDataPackage = await SendAsync(DeviceCommand.RequestTransferFirmwareUpgrade, data.ToArray(),
            cancellationToken);
        if (netDataPackage == null)
            return false;

        return BitConverter.ToInt16(netDataPackage.Data.Skip(12).Take(2).ToArray()) == 0;
    }

    private Dictionary<Parameter.DataChannelType, double[,]> ProcessChannelData(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var channelType = (Parameter.DataChannelType)reader.ReadInt32();
        var dataLength = reader.ReadInt32();
        var result = new Dictionary<Parameter.DataChannelType, double[,]>();

        switch (channelType)
        {
            case Parameter.DataChannelType.Velocity:
            case Parameter.DataChannelType.Displacement:
            case Parameter.DataChannelType.Acceleration:
                result.Add(channelType, new double[1, dataLength]);
                for (var i = 0; i < dataLength; i++)
                    result[channelType][0, i] = reader.ReadSingle();
                break;
            case Parameter.DataChannelType.ISignalAndQSignal:
                result.Add(channelType, new double[2, dataLength]);
                for (var i = 0; i < dataLength; i++)
                {
                    result[channelType][0, i] = reader.ReadInt16();
                    result[channelType][1, i] = reader.ReadInt16();
                }

                break;
        }

        return result;
    }
}