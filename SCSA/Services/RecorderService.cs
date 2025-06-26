using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SCSA.Models;
using SCSA.Services.Recording;
using SCSA.Services.Recording.Writers;
using SCSA.ViewModels;
using SCSA.UFF;

namespace SCSA.Services;

public class RecorderService : IRecorderService
{
    private readonly IAppSettingsService _appSettingsService;
    //private readonly List<short> _cmdIds = new(); // 添加CmdId列表
    private readonly Channel<Dictionary<DataChannelType, double[,]>> _dataChannel;
    private CancellationTokenSource _cancellationTokenSource;
    private List<IChannelWriter> _channelWriters;
    private List<string> _currentFileNames;
    private List<FileStream> _currentFileStreams;
    private DataChannelType _currentSignalType;

    private bool _isRecording;
    private Task _processingTask;

    private double _sampleRate;
    private bool _saveAsUFF;
    private IProgress<int> _saveProgress;

    private StorageType _storageType;
    private long _targetDataLength;
    private long _totalWDataPoints;
    private bool _uffStreamWritersInitialized;
    private bool _useBinaryUFF;
    public string StoragePath { set; get; }
    // 同步锁用于防止 StopRecordingAsync 并发执行
    private readonly object _stopLock = new();

    // 添加数据采集完成事件
    public event EventHandler DataCollectionCompleted;

    public RecorderService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
        try
        {
            StoragePath = _appSettingsService.Load().DataStoragePath;
        }
        catch (Exception e)
        {
            SCSA.Utils.Log.Error("读取数据存储路径失败，将使用默认路径", e);
            StoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }
        finally
        {
            if (!Directory.Exists(StoragePath)) Directory.CreateDirectory(StoragePath);
        }

        _dataChannel = Channel.CreateUnbounded<Dictionary<DataChannelType, double[,]>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    }

    public async Task<long> StartRecordingAsync(DataChannelType signalType, TriggerType triggerType,
        double sampleRate, IProgress<int> saveProgress, long targetDataLength = 0,
        double targetTimeSeconds = 0,
        StorageType storageType = StorageType.Length, FileFormatType fileFormat = FileFormatType.Binary,
        bool useBinaryUFF = true)
    {
        try
        {
            _saveProgress = saveProgress;
            _sampleRate = sampleRate;
            _currentSignalType = signalType;
            _storageType = storageType;
            _saveAsUFF = fileFormat == FileFormatType.UFF;
            _useBinaryUFF = useBinaryUFF;
            //_cmdIds.Clear();

            _targetDataLength = CalculateTargetDataLength(triggerType, targetDataLength, targetTimeSeconds);

            var timestamp = InitializeRecordingStateAndGetTimestamp();
            var fileNameSuffix = GenerateFileNameSuffix(triggerType, _targetDataLength, targetTimeSeconds, timestamp);
            await InitializeWritersAsync(signalType, fileFormat, fileNameSuffix);

            SCSA.Utils.Log.Info($"目标数据长度: {_targetDataLength}");
            SCSA.Utils.Log.Info($"采样率: {_sampleRate}");
            SCSA.Utils.Log.Info($"预期时间: {targetTimeSeconds}秒");
            SCSA.Utils.Log.Info($"存储类型: {_storageType}");
            SCSA.Utils.Log.Info($"触发类型: {triggerType}");

            //var cmdIdFileName = Path.Combine(StoragePath, $"CmdId_{fileNameSuffix}.txt");

            _totalWDataPoints = 0;
            _uffStreamWritersInitialized = false;
            _saveProgress?.Report(0);

            while (_dataChannel.Reader.TryRead(out _))
            {
                // Empty loop to discard stale data
            }

            _isRecording = true;

            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = ProcessDataAsync(_cancellationTokenSource.Token);
            return _targetDataLength;
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"启动记录失败: {ex.Message}");
            SCSA.Utils.Log.Error("启动记录失败", ex);
            throw;
        }
    }

    // 添加记录CmdId的方法
    //public void RecordCmdId(short cmdId)
    //{
    //    if (_isRecording) _cmdIds.Add(cmdId);
    //}

    // 接口实现，默认等待处理线程结束
    public Task StopRecordingAsync() => StopRecordingAsyncInternal(true);

    // 内部重载，可选择是否等待 _processingTask
    private async Task StopRecordingAsyncInternal(bool waitForProcessing)
    {
        // 确保仅有一次真正进入停止逻辑
        lock (_stopLock)
        {
            if (!_isRecording)
                return;

            _isRecording = false;
        }

        _cancellationTokenSource?.Cancel();

        try
        {
            if (waitForProcessing && _processingTask != null)
            {
                if (!Task.CurrentId.HasValue || _processingTask.Id != Task.CurrentId.Value)
                {
                    await _processingTask;
                }
            }

            //// 保存CmdId列表
            //if (_cmdIds.Count > 0)
            //{
            //    var cmdIdFileName = Path.Combine(StoragePath, $"CmdId_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            //    await File.WriteAllLinesAsync(cmdIdFileName, _cmdIds.Select(id => id.ToString()));
            //}

            foreach (var writer in _channelWriters) writer?.Dispose();

            foreach (var stream in _currentFileStreams) stream?.Dispose();

            _channelWriters.Clear();
            _currentFileStreams.Clear();
            _currentFileNames.Clear();
            //_cmdIds.Clear();
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"停止记录失败: {ex.Message}");
            SCSA.Utils.Log.Error("停止记录失败", ex);
        }
        finally
        {
            _isRecording = false;
            DataCollectionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<bool> WriteDataAsync(Dictionary<DataChannelType, double[,]> channelDatas)
    {
        if (!_isRecording)
            return false;

        await _dataChannel.Writer.WriteAsync(channelDatas);

        return true;
    }

    private string InitializeRecordingStateAndGetTimestamp()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _currentFileStreams = new List<FileStream>();
        _channelWriters = new List<IChannelWriter>();
        _currentFileNames = new List<string>();
        return timestamp;
    }

    private long CalculateTargetDataLength(TriggerType triggerType, long targetDataLength, double targetTimeSeconds)
    {
        if (triggerType == TriggerType.DebugTrigger)
        {
            _storageType = StorageType.Length;
            return 64 * 1024 * 1024 / 4;
        }

        if (_storageType == StorageType.Time)
        {
            return (long)(targetTimeSeconds * _sampleRate);
        }

        return targetDataLength;
    }

    private string GenerateFileNameSuffix(TriggerType triggerType, long targetDataLength, double targetTimeSeconds,
        string timestamp)
    {
        if (triggerType == TriggerType.DebugTrigger) return $"{_sampleRate}_{timestamp}";

        var storageTypeStr = _storageType == StorageType.Length ? "ByLength" : "ByTime";
        var targetInfo = _storageType == StorageType.Length
            ? $"Len{targetDataLength}"
            : $"Time{targetTimeSeconds}s";
        return $"SR{_sampleRate}_{storageTypeStr}_{targetInfo}_{triggerType}_{timestamp}";
    }

    private Task InitializeWritersAsync(DataChannelType signalType, FileFormatType fileFormat,
        string fileNameSuffix)
    {
        try
        {
            var fileExtension = GetFileExtension(fileFormat);
            var saveAsWav = fileFormat == FileFormatType.WAV;

            if (signalType == DataChannelType.ISignalAndQSignal)
            {
                if (_saveAsUFF)
                {
                    var iqFileName = Path.Combine(StoragePath, $"IQ_Signal_{fileNameSuffix}.{fileExtension}");
                    _currentFileNames.Add(iqFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(iqFileName));

                    string unit = "V";
                    var uffStreamWriterI = new UFFStreamWriter(iqFileName,
                        _useBinaryUFF ? UFFWriteFormat.Binary : UFFWriteFormat.ASCII,
                        _sampleRate, append: false, useFloat32: false, ordinateUnit: unit);
                    var uffStreamWriterQ = new UFFStreamWriter(iqFileName,
                        _useBinaryUFF ? UFFWriteFormat.Binary : UFFWriteFormat.ASCII,
                        _sampleRate, append: true, useFloat32: false, ordinateUnit: unit);

                    _channelWriters.Add(new UffChannelWriter(uffStreamWriterI, leaveOpen: false)); // I
                    _channelWriters.Add(new UffChannelWriter(uffStreamWriterQ, leaveOpen: false));  // Q

                    _currentFileStreams.Add(null);
                    _currentFileStreams.Add(null);
                }
                else
                {
                    var iFileName = Path.Combine(StoragePath, $"I_Signal_{fileNameSuffix}.{fileExtension}");
                    var qFileName = Path.Combine(StoragePath, $"Q_Signal_{fileNameSuffix}.{fileExtension}");
                    _currentFileNames.Add(iFileName);
                    _currentFileNames.Add(qFileName);

                    foreach (var fileName in _currentFileNames)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        var writer = new BinaryWriter(fileStream);
                        _currentFileStreams.Add(fileStream);
                        if (saveAsWav)
                            _channelWriters.Add(new WavChannelWriter(writer, (int)_sampleRate));
                        else
                            _channelWriters.Add(new BinChannelWriter(writer,
                                _currentSignalType == DataChannelType.ISignalAndQSignal
                                    ? BinDataType.Int16
                                    : BinDataType.Float32));
                    }
                }
            }
            else
            {
                var fileName = Path.Combine(StoragePath, $"{signalType}_{fileNameSuffix}.{fileExtension}");
                _currentFileNames.Add(fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                if (_saveAsUFF)
                {
                    string unit = signalType switch
                    {
                        DataChannelType.Velocity => "mm/s",
                        DataChannelType.Displacement => "um",
                        DataChannelType.Acceleration => "m/s2",
                        _ => "V"
                    };
                    var uffStreamWriter = new UFFStreamWriter(fileName,
                        _useBinaryUFF ? UFFWriteFormat.Binary : UFFWriteFormat.ASCII,
                        _sampleRate, append: false, useFloat32: false, ordinateUnit: unit);
                    _currentFileStreams.Add(null);
                    _channelWriters.Add(new UffChannelWriter(uffStreamWriter));
                }
                else
                {
                    var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    var writer = new BinaryWriter(fileStream);
                    _currentFileStreams.Add(fileStream);
                    _channelWriters.Add(saveAsWav
                        ? new WavChannelWriter(writer, (int)_sampleRate)
                        : new BinChannelWriter(writer, BinDataType.Float32));
                }
            }
        }
        catch (Exception ex)
        {
            SCSA.Utils.Log.Error("初始化记录器写入程序失败", ex);
            throw;
        }

        return Task.CompletedTask;
    }

    private string GetFileExtension(FileFormatType fileFormat)
    {
        return fileFormat switch
        {
            FileFormatType.UFF => "uff",
            FileFormatType.WAV => "wav",
            _ => "bin"
        };
    }

    private async Task ProcessDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var channelData in _dataChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    foreach (var (channelType, data) in channelData)
                        if (channelType == _currentSignalType)
                        {
                            var perChannelDataLen = data.GetLength(1);
                            var remainDataLen = _targetDataLength - _totalWDataPoints;
                            if (remainDataLen >= perChannelDataLen)
                                remainDataLen = perChannelDataLen;

                            if (_currentSignalType == DataChannelType.ISignalAndQSignal)
                            {
                                var iSignal = new double[data.GetLength(1)];
                                var qSignal = new double[data.GetLength(1)];
                                for (var j = 0; j < data.GetLength(1); j++)
                                {
                                    iSignal[j] = data[0, j];
                                    qSignal[j] = data[1, j];
                                }

                                _channelWriters[0].Write(iSignal.Take((int)remainDataLen).ToArray());
                                _channelWriters[1].Write(qSignal.Take((int)remainDataLen).ToArray());
                            }
                            else
                            {
                                var signal = new double[data.Length];
                                for (var j = 0; j < data.Length; j++) signal[j] = data[0, j];
                                _channelWriters[0].Write(signal.Take((int)remainDataLen).ToArray());
                            }

                            _totalWDataPoints += remainDataLen;

                            var progressPercent = (int)((double)_totalWDataPoints / _targetDataLength * 100);
                            _saveProgress?.Report(progressPercent);

                            if (_totalWDataPoints >= _targetDataLength)
                            {
                                await StopRecordingAsyncInternal(false);
                                break;
                            }
                        }
                }
                catch (Exception ex)
                {
                    SCSA.Utils.Log.Error("写入数据块失败", ex);
                    await StopRecordingAsyncInternal(false);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            //Debug.WriteLine("数据处理任务被取消。");
            SCSA.Utils.Log.Info("数据处理任务被取消。");
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"处理数据时出错: {ex.Message}");
            SCSA.Utils.Log.Error("处理数据时出错", ex);
            await StopRecordingAsyncInternal(false);
        }
        finally
        {
            //if (_saveAsUFF) await CleanupUFFStreamWritersAsync();
        }
    }




}