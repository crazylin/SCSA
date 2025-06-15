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
using SCSA.UFF;
using SCSA.UFF.Models;
using SCSA.ViewModels;

namespace SCSA.Services;

public class RecorderService : IRecorderService
{
    private readonly IAppSettingsService _appSettingsService;
    //private readonly List<short> _cmdIds = new(); // 添加CmdId列表
    private readonly Channel<Dictionary<Parameter.DataChannelType, double[,]>> _dataChannel;

    private readonly List<UFFStreamWriter> _uffStreamWriters = new();
    private readonly SemaphoreSlim _uffWriteSemaphore = new(1, 1);
    private CancellationTokenSource _cancellationTokenSource;
    private List<IChannelWriter> _channelWriters;
    private List<string> _currentFileNames;
    private List<FileStream> _currentFileStreams;
    private Parameter.DataChannelType _currentSignalType;

    private bool _isRecording;
    private Task _processingTask;
    private IProgress<int> _receivedProgress;

    private double _sampleRate;
    private bool _saveAsUFF;
    private IProgress<int> _saveProgress;

    private DateTime _startTime;
    private StorageType _storageType;
    private long _targetDataLength;
    private long _totalWDataPoints;
    private long _totalRDataPoints;
    private bool _uffStreamWritersInitialized;
    private bool _useBinaryUFF;
    public string StoragePath { set; get; }
    // 同步锁用于防止 StopRecordingAsync 并发执行
    private readonly object _stopLock = new();

    public RecorderService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
        StoragePath = _appSettingsService.Load().DataStoragePath;
        _dataChannel = Channel.CreateUnbounded<Dictionary<Parameter.DataChannelType, double[,]>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    }



    // 添加数据采集完成事件
    public event EventHandler DataCollectionCompleted;

    public async Task StartRecordingAsync(Parameter.DataChannelType signalType, TriggerType triggerType,
        double sampleRate, IProgress<int> saveProgress, IProgress<int> receivedProgress, long targetDataLength = 0,
        double targetTimeSeconds = 0,
        StorageType storageType = StorageType.Length, FileFormatType fileFormat = FileFormatType.Binary,
        bool useBinaryUFF = true)
    {
        try
        {
            _saveProgress = saveProgress;
            _receivedProgress = receivedProgress;
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

            Debug.WriteLine($"目标数据长度: {_targetDataLength}");
            Debug.WriteLine($"采样率: {_sampleRate}");
            Debug.WriteLine($"预期时间: {targetTimeSeconds}秒");
            Debug.WriteLine($"存储类型: {_storageType}");
            Debug.WriteLine($"触发类型: {triggerType}");

            //var cmdIdFileName = Path.Combine(StoragePath, $"CmdId_{fileNameSuffix}.txt");

            _totalWDataPoints = 0;
            _totalRDataPoints = 0;
            _uffStreamWritersInitialized = false;
            _startTime = DateTime.Now;
            _saveProgress?.Report(0);
            _receivedProgress.Report(0);

            while (_dataChannel.Reader.TryRead(out _))
            {
                // Empty loop to discard stale data
            }

            _isRecording = true;

            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = ProcessDataAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动记录失败: {ex.Message}");
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
            Debug.WriteLine($"停止记录失败: {ex.Message}");
        }
        finally
        {
            _isRecording = false;
            DataCollectionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<bool> WriteDataAsync(Dictionary<Parameter.DataChannelType, double[,]> channelDatas)
    {
        if (!_isRecording)
            return false;
        if (_dataChannel.Writer.TryWrite(channelDatas))
        {

            _totalRDataPoints += channelDatas.ElementAt(0).Value.GetLength(1);
            var enProgress = (int)((double)_totalRDataPoints / _targetDataLength * 100);
            _receivedProgress?.Report(enProgress);
            return true;
        }

        return false;
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

    private async Task InitializeWritersAsync(Parameter.DataChannelType signalType, FileFormatType fileFormat,
        string fileNameSuffix)
    {
        var fileExtension = GetFileExtension(fileFormat);
        var saveAsWav = fileFormat == FileFormatType.WAV;

        if (signalType == Parameter.DataChannelType.ISignalAndQSignal)
        {
            if (_saveAsUFF)
            {
                var iqFileName = Path.Combine(StoragePath, $"IQ_Signal_{fileNameSuffix}.{fileExtension}");
                _currentFileNames.Add(iqFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(iqFileName));
                _currentFileStreams.Add(null);
                _channelWriters.Add(null);
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
                            _currentSignalType == Parameter.DataChannelType.ISignalAndQSignal
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
                _currentFileStreams.Add(null);
                _channelWriters.Add(null);
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

                foreach (var (channelType, data) in channelData)
                    if (channelType == _currentSignalType)
                    {
                        var perChannelDataLen = data.GetLength(1);
                        var remainDataLen = _targetDataLength - _totalWDataPoints;
                        if (remainDataLen >= perChannelDataLen)
                            remainDataLen = perChannelDataLen;
                            
                        //if (_saveAsUFF)
                        //{
                        //    if (!_uffStreamWritersInitialized)
                        //    {
                        //        var channelCount = _currentSignalType == Parameter.DataChannelType.ISignalAndQSignal
                        //            ? 2
                        //            : 1;
                        //        await InitializeUFFStreamWritersAsync(channelCount);
                        //        _uffStreamWritersInitialized = true;
                        //    }

                        //    var dataList = new List<double[]>();
                        //    if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
                        //    {
                        //        var iSignal = new double[data.GetLength(1)];
                        //        var qSignal = new double[data.GetLength(1)];
                        //        for (var i = 0; i < data.GetLength(1); i++)
                        //        {
                        //            iSignal[i] = data[0, i];
                        //            qSignal[i] = data[1, i];
                        //        }

                        //        dataList.Add(iSignal);
                        //        dataList.Add(qSignal);
                        //    }
                        //    else
                        //    {
                        //        var signal = new double[data.Length];
                        //        Buffer.BlockCopy(data, 0, signal, 0, data.Length * sizeof(double));
                        //        dataList.Add(signal);
                        //    }

                        //    await WriteUFFData(dataList);
                        //}
                        //else
                        {
                            if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
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
                        }

                        var progressPercent = (int)((double)_totalWDataPoints / _targetDataLength * 100);
                        _saveProgress?.Report(progressPercent);

                        if (_totalWDataPoints >= _targetDataLength)
                        {
                            await StopRecordingAsyncInternal(false);
                            break;
                        }
                    }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("数据处理任务被取消。");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理数据时出错: {ex.Message}");
        }
        finally
        {
            //if (_saveAsUFF) await CleanupUFFStreamWritersAsync();
        }
    }




}