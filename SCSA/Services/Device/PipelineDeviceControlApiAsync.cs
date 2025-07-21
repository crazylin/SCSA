using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Utils;
using System.Security.Cryptography;

namespace SCSA.Services.Device;

public class PipelineDeviceControlApiAsync : IDisposable
{
    private readonly DeviceControlSetting _deviceControlSetting;

    // 使用CmdId来匹配等待命令，避免命令冲突
    private readonly ConcurrentDictionary<short, TaskCompletionSource<PipelineNetDataPackage>> _pendingCommands;
    
    // 添加命令创建时间跟踪
    private readonly ConcurrentDictionary<short, DateTime> _commandCreationTimes;

    private readonly PipelineTcpClient<PipelineNetDataPackage> _tcpClient;

    private short _flag;
 
    
    // 添加取消令牌和清理任务
    private readonly CancellationTokenSource _cleanupCts;
    private Task? _cleanupTask;
    private bool _disposed;

    // 升级请求(TC 0xFA)等待器
    private TaskCompletionSource<bool>? _upgradeRequestTcs;
    private readonly object _upgradeRequestLock = new();

    public PipelineDeviceControlApiAsync(PipelineTcpClient<PipelineNetDataPackage> tcpClient)
    {
        _tcpClient = tcpClient;
        tcpClient.DataReceived += TcpClient_DataReceived;
        _pendingCommands = new ConcurrentDictionary<short, TaskCompletionSource<PipelineNetDataPackage>>();
        _commandCreationTimes = new ConcurrentDictionary<short, DateTime>();

        
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

    public event Action<Dictionary<DataChannelType, double[,]>> DataReceived;

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
            case DeviceCommand.ReplyGetParameterIds:
            case DeviceCommand.ReplyGetDeviceStatus:
            case DeviceCommand.ReplyStartFirmwareUpgrade:
            case DeviceCommand.ReplyTransferFirmwareUpgrade:
            case DeviceCommand.ReplySendFirmwareInfo:
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
                DataReceived?.Invoke(ProcessChannelData(netDataPackage.Data));
                break;

            case DeviceCommand.DeviceRequestFirmwareUpgrade:
                // 设备发起的升级请求(0xFA)
                lock (_upgradeRequestLock)
                {
                    _upgradeRequestTcs?.TrySetResult(true);
                }
                break;

            default:
                Log.Error($"未知的命令类型: {netDataPackage.DeviceCommand}");
                break;
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
                         }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            _pendingCommands.TryRemove(netPackage.CmdId, out _);
            _commandCreationTimes.TryRemove(netPackage.CmdId, out _);

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
        var data = Parameter.Get_GetParametersData(parameters);

        var netDataPackage = await SendAsync(DeviceCommand.RequestReadParameters, data, cancellationToken);
        if (netDataPackage == null)
            return (false, parameters);
    
        var result = BitConverter.ToInt16(netDataPackage.Data) == 0;
        if (!result)
            return (false, new List<Parameter>());
        var resultParameters = Parameter.Get_GetParametersResult(netDataPackage.Data.Skip(2).ToArray());
        var returnParameters = new List<Parameter>();
        foreach (var resultParameter in resultParameters)
        {
            var p = parameters.FirstOrDefault(p => p.Address == resultParameter.Address);
            if (p != null)
            {
                p.Value = resultParameter.ToParameterData();
                returnParameters.Add(p);
            }
        }

        return (result, returnParameters);
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


    public async Task<(bool success, List<Parameter> result)> GetParameterIds(CancellationToken cancellationToken)
    {
        var data = Array.Empty<byte>();

        var netDataPackage = await SendAsync(DeviceCommand.RequestGetParameterIds, data, cancellationToken);
        if (netDataPackage == null)
            return (false,  new List<Parameter>());

        var result = BitConverter.ToInt16(netDataPackage.Data) == 0;
        if (!result)
            return (false, new List<Parameter>());

        var resultParameters = Parameter.Get_GetParameterIdsResult(netDataPackage.Data.Skip(2).ToArray());

        return (result, resultParameters);
    }

    public async Task<(bool success, List<DeviceStatus> result)> GetDeviceStatus(List<DeviceStatusType> deviceStatusList, CancellationToken cancellationToken)
    {
        var data = DeviceStatus.Get_GetDeviceStatusData(deviceStatusList);

        var netDataPackage = await SendAsync(DeviceCommand.RequestGetDeviceStatus, data, cancellationToken);
        if (netDataPackage == null)
            return (false, new List<DeviceStatus>());

        var result = BitConverter.ToInt16(netDataPackage.Data) == 0;
        if (!result)
            return (false, new List<DeviceStatus>());

        var resultDeviceStatus = DeviceStatus.Get_GetDeviceStatusResult(netDataPackage.Data.Skip(2).ToArray());

        return (result, resultDeviceStatus);
    }
    

    /// <summary>
    /// 发送 0xF8 启动升级命令
    /// </summary>
    public async Task<bool> FirmwareUpgradeStart(CancellationToken cancellationToken)
    {
        // 新协议中该命令无数据负载
        var netDataPackage = await SendAsync(DeviceCommand.RequestStartFirmwareUpgrade, Array.Empty<byte>(),
            cancellationToken);
        if (netDataPackage == null)
            return false;
        return BitConverter.ToInt16(netDataPackage.Data) == 0;
    }

    /// <summary>
    /// 发送 0xFB 固件参数 (加密 CRC32 + 固件大小)
    /// </summary>
    public async Task<bool> FirmwareUpgradeSendInfo(byte[] firmwareData, CancellationToken cancellationToken)
    {
        // 计算 CRC32
        uint crc32 = System.IO.Hashing.Crc32.HashToUInt32(firmwareData);

        var crcBytes = BitConverter.GetBytes(crc32);

        // AES-256 CTR 加密 4 字节 CRC
        byte[] key =
        {
            0x7A, 0x4F, 0x92, 0x1D, 0x3C, 0x88, 0x65, 0x4E, 0xE2, 0x1B, 0x07, 0x9A, 0x5F, 0x33, 0x4C, 0x78,
            0xD9, 0x6A, 0x12, 0x8B, 0x40, 0xFF, 0x91, 0x06, 0x3E, 0x5D, 0x87, 0x2C, 0x19, 0xBE, 0x50, 0xA3
        };

        byte[] iv =
        {
            0x5E, 0x73, 0x9A, 0x2F, 0x4D, 0x18, 0x6C, 0xB7, 0xA1, 0x0C, 0x3F, 0x85, 0x29, 0x6E, 0x14, 0xD3
        };

        byte[] encryptedCrc = EncryptAesCtr(crcBytes, key, iv);

        var data = new List<byte>();
        data.AddRange(encryptedCrc);
        data.AddRange(BitConverter.GetBytes(firmwareData.Length));

        var netDataPackage = await SendAsync(DeviceCommand.RequestSendFirmwareInfo, data.ToArray(),
            cancellationToken);
        if (netDataPackage == null)
            return false;

        // 返回数据：2 Byte 应答码，0 为成功
        return netDataPackage.Data.Length >= 2 && BitConverter.ToInt16(netDataPackage.Data, 0) == 0;
    }

    /// <summary>
    /// 发送 0xFD 固件数据包
    /// </summary>
    public async Task<bool> FirmwareUpgradeTransfer(int packageId, byte[] firmwareData,
        CancellationToken cancellationToken)
    {
        var data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(packageId));
        data.AddRange(BitConverter.GetBytes(firmwareData.Length));
        data.AddRange(firmwareData);

        var netDataPackage = await SendAsync(DeviceCommand.RequestTransferFirmwareUpgrade, data.ToArray(),
            cancellationToken);
        if (netDataPackage == null)
            return false;

        // 应答包结构: 包ID(4) + 包长度(4) + 应答(2)
        return netDataPackage.Data.Length >= 10 && BitConverter.ToInt16(netDataPackage.Data, 8) == 0;
    }

    public static byte[] EncryptAesCtr(byte[] plain, byte[] key, byte[] iv)
    {
        if (plain.Length > 16)
            throw new ArgumentException("CTR 加密辅助函数仅支持 16 字节以内的数据", nameof(plain));

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.Mode = System.Security.Cryptography.CipherMode.ECB;
        aes.Padding = System.Security.Cryptography.PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        // 生成 16 字节密钥流块
        var keystreamBlock = encryptor.TransformFinalBlock(iv, 0, iv.Length);

        var cipher = new byte[plain.Length];
        for (int i = 0; i < plain.Length; i++)
        {
            cipher[i] = (byte)(plain[i] ^ keystreamBlock[i]);
        }

        return cipher;
    }

    private Dictionary<DataChannelType, double[,]> ProcessChannelData(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var channelType = (DataChannelType)reader.ReadInt32();
        var dataLength = reader.ReadInt32();
        var result = new Dictionary<DataChannelType, double[,]>();

        switch (channelType)
        {
            case DataChannelType.Velocity:
            case DataChannelType.Displacement:
            case DataChannelType.Acceleration:
                result.Add(channelType, new double[1, dataLength]);
                for (var i = 0; i < dataLength; i++)
                    result[channelType][0, i] = reader.ReadSingle();
                break;
            case DataChannelType.ISignalAndQSignal:
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

    public async Task<bool> WaitForDeviceRequestFirmwareUpgrade(CancellationToken cancellationToken)
    {
        lock (_upgradeRequestLock)
        {
            _upgradeRequestTcs = new TaskCompletionSource<bool>();
        }

        try
        {
            await using (cancellationToken.Register(() =>
                     {
                         lock (_upgradeRequestLock)
                         {
                             _upgradeRequestTcs?.TrySetCanceled();
                         }
                     }))
            {
                return await _upgradeRequestTcs.Task.ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}