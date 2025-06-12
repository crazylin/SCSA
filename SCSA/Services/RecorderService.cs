using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using SCSA.Models;
using Avalonia.Threading;
using SCSA.Utils;
using SCSA.ViewModels;

namespace SCSA.Services
{
    public class RecorderService
    {
        public string StoragePath { set; get; }
        private readonly IProgress<int> _progress;
        private CancellationTokenSource _cancellationTokenSource;
        private List<FileStream> _currentFileStreams;
        private List<BinaryWriter> _currentWriters;
        private List<string> _currentFileNames;
        private long _totalDataPoints = 0;
        private long _enTotalDataPoints = 0;
        private long _targetDataLength = 0;
        private int _targetTimeSeconds = 0;
        private DateTime _startTime;
        private Channel<Dictionary<Parameter.DataChannelType, double[,]>> _dataChannel;
        private Task _processingTask;
        private Parameter.DataChannelType _currentSignalType;
        private bool _isRecording = false;
        private StorageType _storageType;

        private double _sampleRate;

        // 添加数据采集完成事件
        public event EventHandler DataCollectionCompleted;

        public RecorderService(string storagePath, IProgress<int> progress = null)
        {
            StoragePath = storagePath;
            _progress = progress;
            _dataChannel = Channel.CreateUnbounded<Dictionary<Parameter.DataChannelType, double[,]>>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        }

        public async Task StartRecordingAsync(Parameter.DataChannelType signalType, TriggerType triggerType, double sampleRate, long targetDataLength = 0, int targetTimeSeconds = 0, StorageType storageType = StorageType.ByLength)
        {
            try
            {
                _sampleRate = sampleRate;
                _currentSignalType = signalType;
                _storageType = storageType;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentFileStreams = new List<FileStream>();
                _currentWriters = new List<BinaryWriter>();
                _currentFileNames = new List<string>();

                // 如果是调试触发模式，使用固定的数据长度
                if (triggerType == TriggerType.DebugTrigger)
                {
                    _targetDataLength = 64 * 1024 * 1024 / 4; // 64M/4
                    _storageType = StorageType.ByLength;
                }
                else
                {
                    _targetDataLength = targetDataLength;
                    _targetTimeSeconds = targetTimeSeconds;
                }

                if (signalType == Parameter.DataChannelType.ISignalAndQSignal)
                {
                    // IQ信号需要两个文件
                    var iFileName = Path.Combine(StoragePath, $"I_Signal_{sampleRate}_{timestamp}.bin");
                    var qFileName = Path.Combine(StoragePath, $"Q_Signal_{sampleRate}_{timestamp}.bin");
                    
                    _currentFileNames.Add(iFileName);
                    _currentFileNames.Add(qFileName);

                    foreach (var fileName in _currentFileNames)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        var writer = new BinaryWriter(fileStream);
                        _currentFileStreams.Add(fileStream);
                        _currentWriters.Add(writer);
                    }
                }
                else
                {
                    // 其他信号类型只需要一个文件
                    var fileName = Path.Combine(StoragePath, $"{signalType}_{sampleRate}_{timestamp}.bin");
                    _currentFileNames.Add(fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    var writer = new BinaryWriter(fileStream);
                    _currentFileStreams.Add(fileStream);
                    _currentWriters.Add(writer);
                }
                
                _totalDataPoints = 0;
                _enTotalDataPoints = 0;
                _startTime = DateTime.Now;
                _progress?.Report(0);
                _isRecording = true;

                // 启动数据处理任务
                _cancellationTokenSource = new CancellationTokenSource();
                _processingTask = ProcessDataAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                await StopRecordingAsync();
                throw new Exception($"启动记录失败: {ex.Message}", ex);
            }
        }

        public async Task StopRecordingAsync()
        {
            try
            {
                _isRecording = false;
                _cancellationTokenSource?.Cancel();
                
                if (_processingTask != null)
                {
                    await _processingTask;
                }

                if (_currentWriters != null)
                {
                    foreach (var writer in _currentWriters)
                    {
                        if (writer != null)
                        {
                            writer.Close();
                        }
                    }
                    _currentWriters.Clear();
                }
                
                if (_currentFileStreams != null)
                {
                    foreach (var stream in _currentFileStreams)
                    {
                        if (stream != null)
                        {
                            stream.Close();
                        }
                    }
                    _currentFileStreams.Clear();
                }
                
                _progress?.Report(100);
            }
            catch (Exception ex)
            {
                throw new Exception($"停止记录失败: {ex.Message}", ex);
            }
        }

        public async Task WriteDataAsync(Dictionary<Parameter.DataChannelType, double[,]> channelDatas)
        {
            if (!_isRecording)
            {
                throw new InvalidOperationException("记录未启动");
            }

            await _dataChannel.Writer.WriteAsync(channelDatas);

            //_enTotalDataPoints += channelDatas.ElementAt(0).Value.GetLength(1);
            //// 检查是否达到目标
            //if (_storageType == StorageType.ByLength)
            //{
            //    return _enTotalDataPoints >= _targetDataLength;
            //}
            //else // ByTime
            //{
            //    return _enTotalDataPoints >= (int)(_targetTimeSeconds * _sampleRate);
            //}
        }

        private async Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var channelDatas = await _dataChannel.Reader.ReadAsync(cancellationToken);
                    
                    if (channelDatas.TryGetValue(_currentSignalType, out var channelData))
                    {
                        // 检查是否达到目标
                        bool shouldStop = false;
                        if (_storageType == StorageType.ByLength)
                        {
                            var remainingPoints = _targetDataLength - _enTotalDataPoints;
                            var currentDataLength = channelData.GetLength(1);

                            if (remainingPoints <= currentDataLength)
                            {
                              
                                shouldStop = true;
                                if (remainingPoints > 0)
                                {
                                    _enTotalDataPoints += remainingPoints;
                                    if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
                                    {
                                        var dataList = new List<Int16[]>();
                                        for (int i = 0; i < channelData.GetLength(0); i++)
                                        {
                                            var row = channelData.GetRow(i);
                                            dataList.Add(row.Take((int)remainingPoints).Select(d=>(Int16)d).ToArray());
                                        }
                                        WriteData(dataList);
                                    }
                                    else
                                    {
                                        var dataList = new List<float[]>();
                                        for (int i = 0; i < channelData.GetLength(0); i++)
                                        {
                                            var row = channelData.GetRow(i);
                                            dataList.Add(row.Take((int)remainingPoints).Select(d => (float)d).ToArray());
                                        }
                                        WriteData(dataList);
                                    }
                                }
                            }
                            else
                            {
                                _enTotalDataPoints += currentDataLength;
                                if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
                                {
                                    var dataList = new List<Int16[]>();
                                    for (int i = 0; i < channelData.GetLength(0); i++)
                                    {
                                        dataList.Add(channelData.GetRow(i).Select(d=>(Int16)d).ToArray());
                                    }
                                    WriteData(dataList);
                                }
                                else
                                {
                                    var dataList = new List<float[]>();
                                    for (int i = 0; i < channelData.GetLength(0); i++)
                                    {
                                        dataList.Add(channelData.GetRow(i).Select(d=>(float)d).ToArray());
                                    }
                                    WriteData(dataList);
                                }
                            }
                        }
                        else // ByTime
                        {
                            var targetPoints = (int)(_targetTimeSeconds * _sampleRate);

                            var remainingPoints = targetPoints - _enTotalDataPoints;
                            var currentDataLength = channelData.GetLength(1);

                            if (remainingPoints <= currentDataLength)
                            {
                                shouldStop = true;
                                if (remainingPoints > 0)
                                {
                                    _enTotalDataPoints += remainingPoints;
                                    if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
                                    {
                                        var dataList = new List<Int16[]>();
                                        for (int i = 0; i < channelData.GetLength(0); i++)
                                        {
                                            var row = channelData.GetRow(i);
                                            dataList.Add(row.Take((int)remainingPoints).Select(d=>(Int16)d).ToArray());
                                        }
                                        WriteData(dataList);
                                    }
                                    else
                                    {
                                        var dataList = new List<float[]>();
                                        for (int i = 0; i < channelData.GetLength(0); i++)
                                        {
                                            var row = channelData.GetRow(i);
                                            dataList.Add(row.Take((int)remainingPoints).Select(d => (float)d).ToArray());
                                        }
                                        WriteData(dataList);
                                    }
                                }
                            }
                            else
                            {
                                _enTotalDataPoints += currentDataLength;
                                if (_currentSignalType == Parameter.DataChannelType.ISignalAndQSignal)
                                {
                                    var dataList = new List<Int16[]>();
                                    for (int i = 0; i < channelData.GetLength(0); i++)
                                    {
                                        dataList.Add(channelData.GetRow(i).Select(d=>(Int16)d).ToArray());
                                    }
                                    WriteData(dataList);
                                }
                                else
                                {
                                    var dataList = new List<float[]>();
                                    for (int i = 0; i < channelData.GetLength(0); i++)
                                    {
                                        dataList.Add(channelData.GetRow(i).Select(d=>(float)d).ToArray());
                                    }
                                    WriteData(dataList);
                                }
                            }
                        }

                        // 更新进度
                        if (_storageType == StorageType.ByLength && _targetDataLength > 0)
                        {
                            var progress = (int)((double)_totalDataPoints / _targetDataLength * 100);
                            _progress?.Report(Math.Min(progress, 100));
                        }
                        else if (_storageType == StorageType.ByTime && _targetTimeSeconds > 0)
                        {
                            var targetPoints = (int)(_targetTimeSeconds * _sampleRate);
                            var progress = (int)((double)_totalDataPoints / targetPoints * 100);
                            _progress?.Report(Math.Min(progress, 100));
                        }

                        if (shouldStop)
                        {
                            // 触发数据采集完成事件
                            DataCollectionCompleted?.Invoke(this, EventArgs.Empty);
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不需要处理
            }
            catch (Exception ex)
            {
                // 处理其他异常
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    throw new Exception($"数据处理失败: {ex.Message}", ex);
                });
            }
        }

        public void WriteData(List<double[]> channelData)
        {
            if (_currentWriters == null || _currentWriters.Count == 0)
            {
                throw new InvalidOperationException("记录未启动");
            }

            try
            {
                for (int i = 0; i < channelData.Count; i++)
                {
                    if (i < _currentWriters.Count)
                    {
                        var data = channelData[i];
                        //_currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }
                
                _totalDataPoints += channelData[0].Length;
            }
            catch (Exception ex)
            {
                throw new Exception($"写入数据失败: {ex.Message}", ex);
            }
        }

        public void WriteData(List<float[]> channelData)
        {
            if (_currentWriters == null || _currentWriters.Count == 0)
            {
                throw new InvalidOperationException("记录未启动");
            }

            try
            {
                for (int i = 0; i < channelData.Count; i++)
                {
                    if (i < _currentWriters.Count)
                    {
                        var data = channelData[i];
                        //_currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }

                _totalDataPoints += channelData[0].Length;
            }
            catch (Exception ex)
            {
                throw new Exception($"写入数据失败: {ex.Message}", ex);
            }
        }

        public void WriteData(List<Int16[]> channelData)
        {
            if (_currentWriters == null || _currentWriters.Count == 0)
            {
                throw new InvalidOperationException("记录未启动");
            }

            try
            {
                for (int i = 0; i < channelData.Count; i++)
                {
                    if (i < _currentWriters.Count)
                    {
                        var data = channelData[i];
                        //_currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }

                _totalDataPoints += channelData[0].Length;
            }
            catch (Exception ex)
            {
                throw new Exception($"写入数据失败: {ex.Message}", ex);
            }
        }

        public List<string> GetCurrentFileNames()
        {
            return _currentFileNames;
        }

        public long GetTotalDataPoints()
        {
            return _totalDataPoints;
        }

        public long GetEnTotalDataPoints()
        {
            return _enTotalDataPoints;
        }

        public bool IsRecording()
        {
            return _isRecording;
        }

        public bool HasReachedTargetLength()
        {
            return _targetDataLength > 0 && _totalDataPoints >= _targetDataLength;
        }

        public DateTime GetStartTime()
        {
            return _startTime;
        }
    }
} 