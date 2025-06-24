using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

    // 使用CmdId来匹配等待命令，避免命令冲突
    private readonly ConcurrentDictionary<short, TaskCompletionSource<PipelineNetDataPackage>> _pendingCommands;
    
    // 添加命令创建时间跟踪
    private readonly ConcurrentDictionary<short, DateTime> _commandCreationTimes;

    private readonly PipelineTcpClient<PipelineNetDataPackage> _tcpClient;
    private DateTime? _lastDataReceivedTime;
    private short _flag;
    private readonly ConcurrentDictionary<short, DateTime> _firmwareTransferSendTimes;
    
    // 添加取消令牌和清理任务
    private readonly CancellationTokenSource _cleanupCts;
    private Task? _cleanupTask;
    private bool _disposed;

    public PipelineDeviceControlApiAsync(PipelineTcpClient<PipelineNetDataPackage> tcpClient)
    {
        _tcpClient = tcpClient;
        tcpClient.DataReceived += TcpClient_DataReceived;
        _pendingCommands = new ConcurrentDictionary<short, TaskCompletionSource<PipelineNetDataPackage>>();
        _commandCreationTimes = new ConcurrentDictionary<short, DateTime>();
        _firmwareTransferSendTimes = new ConcurrentDictionary<short, DateTime>();
        
        // 初始化取消令牌和启动清理任务
        _cleanupCts = new CancellationTokenSource();
        _cleanupTask = Task.Run(() => CleanupTimeoutCommandsAsync(_cleanupCts.Token));
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _tcpClient.DataReceived -= TcpClient_DataReceived;
            
            // 取消清理任务
            _cleanupCts?.Cancel();
            
            // 等待清理任务完成
            if (_cleanupTask != null)
            {
                try
                {
                    _cleanupTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // 忽略取消异常
                }
            }
            
            // 清理所有等待的命令
            foreach (var kvp in _pendingCommands)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingCommands.Clear();
            _commandCreationTimes.Clear();
            _firmwareTransferSendTimes.Clear();
            
            _cleanupCts?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error("Dispose PipelineDeviceControlApiAsync 时出错", ex);
        }
        finally
        {
            _disposed = true;
        }
    }

    public event Action<Dictionary<Parameter.DataChannelType, double[,]>> DataReceived;

    /// <summary>
    /// 检查对象是否已被释放
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PipelineDeviceControlApiAsync));
        }
    }

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


        switch (netDataPackage.DeviceCommand)
        {
            case DeviceCommand.ReplyStartCollection:
            case DeviceCommand.ReplyStopCollection:
            case DeviceCommand.ReplySetParameters:
            case DeviceCommand.ReplyReadParameters:

            //暂时添加 固件问题
            case DeviceCommand.RequestReadParameters:
            //暂时添加 固件问题
            case DeviceCommand.ReplyStartFirmwareUpgrade:

            case DeviceCommand.ReplyTransferFirmwareUpgrade:
                // 通过CmdId匹配等待的命令
                if (_pendingCommands.TryRemove(netDataPackage.CmdId, out var tcs))
                {
                    tcs.TrySetResult(netDataPackage);
                    // 清理相关记录
                    _commandCreationTimes.TryRemove(netDataPackage.CmdId, out _);
                }
                else
                {
                    Log.Error($"收到未匹配的响应命令: {netDataPackage.DeviceCommand}, CmdId: {netDataPackage.CmdId}");
                }
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
                break;

            default:
                Log.Error($"未知的命令类型: {netDataPackage.DeviceCommand}");
                break;
        }

        // 记录固件传输的往返时间
        if (netDataPackage.DeviceCommand == DeviceCommand.ReplyTransferFirmwareUpgrade)
        {
            var nowTransfer = DateTime.Now;
            if (_firmwareTransferSendTimes.TryRemove(netDataPackage.CmdId, out var sendTime))
            {
                var roundTripTime = nowTransfer - sendTime;
                Log.Info($"FirmwareTransfer round-trip time (CmdId: {netDataPackage.CmdId}): {roundTripTime.TotalMilliseconds:F2} ms");
            }
        }
    }

    public async Task<PipelineNetDataPackage> SendAsync(DeviceCommand command, byte[] data,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        
        var netPackage = new PipelineNetDataPackage();
        netPackage.DeviceCommand = command;
        netPackage.CmdId = _flag++;
        if (_flag > 2048)
            _flag = 0;
        netPackage.Data = data;
        netPackage.DataLen = data.Length;

        //var bytes = netPackage.GetBytes();
        //var hexData = bytes.Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n);
        //Log.Debug($"CMD: {netPackage.DeviceCommand} DataLen {netPackage.DataLen} Data {hexData} Crc {BitConverter.GetBytes(netPackage.Crc).Select(d => d.ToString("x2")).Aggregate((p, n) => p + " " + n)}");

        var tcs = new TaskCompletionSource<PipelineNetDataPackage>();

        // 先注册等待响应，再发送数据
        if (!_pendingCommands.TryAdd(netPackage.CmdId, tcs))
        {
            Log.Error($"CmdId {netPackage.CmdId} 已存在，可能存在重复");
            return null;
        }
        
        // 记录命令创建时间
        _commandCreationTimes.TryAdd(netPackage.CmdId, DateTime.Now);

        // 记录固件传输命令的发送时间
        if (command == DeviceCommand.RequestTransferFirmwareUpgrade)
        {
            _firmwareTransferSendTimes.TryAdd(netPackage.CmdId, DateTime.Now);
        }

        if (!await _tcpClient.SendAsync(netPackage))
        {
            _pendingCommands.TryRemove(netPackage.CmdId, out _);
            return null;
        }

        try
        {
            await using (cancellationToken.Register(() =>
                         {
                             tcs.TrySetCanceled();
                             _pendingCommands.TryRemove(netPackage.CmdId, out _);
                             _commandCreationTimes.TryRemove(netPackage.CmdId, out _);
                             _firmwareTransferSendTimes.TryRemove(netPackage.CmdId, out _);
                         }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            _pendingCommands.TryRemove(netPackage.CmdId, out _);
            _commandCreationTimes.TryRemove(netPackage.CmdId, out _);
            _firmwareTransferSendTimes.TryRemove(netPackage.CmdId, out _);
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

    /// <summary>
    /// 清理超时的等待命令
    /// </summary>
    private async Task CleanupTimeoutCommandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(30000, cancellationToken); // 每30秒清理一次
                    
                    var timeoutCommands = new List<short>();
                    var now = DateTime.Now;
                    
                    foreach (var kvp in _pendingCommands)
                    {
                        var cmdId = kvp.Key;
                        
                        // 检查命令是否已完成或已取消
                        if (kvp.Value.Task.IsCompleted || kvp.Value.Task.IsCanceled)
                        {
                            timeoutCommands.Add(cmdId);
                            continue;
                        }
                        
                        // 检查命令创建时间，如果超过120秒认为超时
                        if (_commandCreationTimes.TryGetValue(cmdId, out var creationTime))
                        {
                            var waitTime = now - creationTime;
                            if (waitTime.TotalSeconds > 120) // 120秒超时
                            {
                                timeoutCommands.Add(cmdId);
                                Log.Error($"命令 {cmdId} 等待超过120秒，认为超时");
                            }
                        }
                    }
                    
                    foreach (var cmdId in timeoutCommands)
                    {
                        if (_pendingCommands.TryRemove(cmdId, out var tcs))
                        {
                            tcs.TrySetCanceled();
                            _commandCreationTimes.TryRemove(cmdId, out _);
                            _firmwareTransferSendTimes.TryRemove(cmdId, out _);
                            Log.Info($"清理超时命令: {cmdId}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("清理超时命令时出错", ex);
                    // 出错后等待一段时间再继续
                    try
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
            Log.Info("清理任务已取消");
        }
        catch (Exception ex)
        {
            Log.Error("清理任务发生严重错误", ex);
        }
    }
}