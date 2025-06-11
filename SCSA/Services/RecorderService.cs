using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using SCSA.Models;

namespace SCSA.Services
{
    public class RecorderService
    {
        private readonly string _storagePath;
        private readonly IProgress<int> _progress;
        private CancellationTokenSource _cancellationTokenSource;
        private List<FileStream> _currentFileStreams;
        private List<BinaryWriter> _currentWriters;
        private List<string> _currentFileNames;
        private long _totalDataPoints = 0;
        private long _targetDataLength = 0;

        public RecorderService(string storagePath, IProgress<int> progress = null)
        {
            _storagePath = storagePath;
            _progress = progress;
        }

        public async Task StartRecordingAsync(Parameter.DataChannelType signalType, double sampleRate, long targetDataLength = 0)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentFileStreams = new List<FileStream>();
                _currentWriters = new List<BinaryWriter>();
                _currentFileNames = new List<string>();

                if (signalType == Parameter.DataChannelType.ISignalAndQSignal)
                {
                    // IQ信号需要两个文件
                    var iFileName = Path.Combine(_storagePath, $"I_Signal_{timestamp}.bin");
                    var qFileName = Path.Combine(_storagePath, $"Q_Signal_{timestamp}.bin");
                    
                    _currentFileNames.Add(iFileName);
                    _currentFileNames.Add(qFileName);

                    foreach (var fileName in _currentFileNames)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        var writer = new BinaryWriter(fileStream);
                        
                        // 写入文件头信息
                        //writer.Write((int)signalType); // 信号类型 (4字节)
                        //writer.Write(sampleRate); // 采样率 (8字节)
                        //writer.Write(0L); // 预留总数据点数位置 (8字节)
                        //writer.Write(targetDataLength); // 目标数据长度 (8字节)
                        
                        _currentFileStreams.Add(fileStream);
                        _currentWriters.Add(writer);
                    }
                }
                else
                {
                    // 其他信号类型只需要一个文件
                    var fileName = Path.Combine(_storagePath, $"{signalType}_{timestamp}.bin");
                    _currentFileNames.Add(fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    var writer = new BinaryWriter(fileStream);
                    
                    // 写入文件头信息
                    //writer.Write((int)signalType); // 信号类型 (4字节)
                    //writer.Write(sampleRate); // 采样率 (8字节)
                    //writer.Write(0L); // 预留总数据点数位置 (8字节)
                    //writer.Write(targetDataLength); // 目标数据长度 (8字节)
                    
                    _currentFileStreams.Add(fileStream);
                    _currentWriters.Add(writer);
                }
                
                _totalDataPoints = 0;
                _targetDataLength = targetDataLength;
                _progress?.Report(0);
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
                if (_currentWriters != null)
                {
                    foreach (var writer in _currentWriters)
                    {
                        if (writer != null)
                        {
                            // 写入总数据点数 (跳过信号类型4字节和采样率8字节)
                            writer.Seek(12, SeekOrigin.Begin);
                            writer.Write(_totalDataPoints);
                            
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
                        _currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }
                
                _totalDataPoints += channelData[0].Length;

                // 更新进度
                if (_targetDataLength > 0)
                {
                    var progress = (int)((double)_totalDataPoints / _targetDataLength * 100);
                    _progress?.Report(Math.Min(progress, 100));
                }
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
                        _currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }

                _totalDataPoints += channelData[0].Length;

                // 更新进度
                if (_targetDataLength > 0)
                {
                    var progress = (int)((double)_totalDataPoints / _targetDataLength * 100);
                    _progress?.Report(Math.Min(progress, 100));
                }
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
                        _currentWriters[i].Write(data.Length);
                        foreach (var value in data)
                        {
                            _currentWriters[i].Write(value);
                        }
                    }
                }

                _totalDataPoints += channelData[0].Length;

                // 更新进度
                if (_targetDataLength > 0)
                {
                    var progress = (int)((double)_totalDataPoints / _targetDataLength * 100);
                    _progress?.Report(Math.Min(progress, 100));
                }
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

        public bool IsRecording()
        {
            return _currentWriters != null && _currentWriters.Count > 0;
        }

        public bool HasReachedTargetLength()
        {
            return _targetDataLength > 0 && _totalDataPoints >= _targetDataLength;
        }
    }
} 