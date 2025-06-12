using OxyPlot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCSA.Models;
using System.Windows.Input;
using Avalonia.Threading;
using MathNet.Numerics.IntegralTransforms;
using OxyPlot.Series;
using System.Timers;
using MathNet.Numerics;
using OxyPlot.Axes;
using SCSA.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.Intrinsics.X86;
using OxyPlot.Legends;
using Timer = System.Timers.Timer;
using QuickMA.Modules.Plot;
using SCSA.Services;
using System.IO;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using SCSA.Services.Filter;

namespace SCSA.ViewModels
{
    public partial class RealTimeTestViewModel : ViewModelBase
    {
        [ObservableProperty]
        private int _displayPointCount = 10240;
        [ObservableProperty]
        private Parameter.DataChannelType _selectedSignalType;
        [ObservableProperty]
        private ObservableCollection<Waveform> _waveforms;
        [ObservableProperty]
        private ObservableCollection<Parameter.DataChannelType> _signalTypes;
        [ObservableProperty]
        private ObservableCollection<WindowFunction> _windowFunctions;
        [ObservableProperty]
        private WindowFunction _selectedWindowFunction;
        [ObservableProperty]
        private bool _removeDc;
        [ObservableProperty]
        private bool _selectedSignalTypeEnable = true;
        [ObservableProperty]
        private double _sampleRate;
        [ObservableProperty]
        private bool _controlEnable = false;
        [ObservableProperty]
        private List<EnumOption> _sampleRateList;
        [ObservableProperty]
        private EnumOption _selectedSampleRate;

        [ObservableProperty] private bool _enableFilter= false;
        [ObservableProperty] private FilterType _filterType = Models.FilterType.LowPass;
        [ObservableProperty] private double _lowPass = 500;
        [ObservableProperty] private double _highPass = 500;
        [ObservableProperty] private double _bandPassFirst = 100;
        [ObservableProperty] private double _bandPassSecond = 500;

        // 新增数据保存相关属性
        [ObservableProperty]
        private bool _isSaving = false;
        [ObservableProperty]
        private double _saveProgress;
        [ObservableProperty]
        private string _saveStatus = "";
        [ObservableProperty]
        private bool _showSaveStatus = true;
        [ObservableProperty]
        private bool _isTestRunning = false;
        [ObservableProperty]
        private string _receivedPoints = "0";
        [ObservableProperty]
        private SolidColorBrush _statusColor = new(Colors.Black);
        [ObservableProperty]
        private bool _showDataStorageInfo;
        [ObservableProperty]
        private string _triggerStatus = "准备就绪";

        private CancellationTokenSource _cts;
        private DispatcherTimer _timer;
        private List<List<double>> _cacheDatas;
        private ConcurrentQueue<List<double[]>> _concurrentQueue;
        private RecorderService _recorderService;
        
        public ConnectionViewModel ConnectionViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { set; get; }

        public RealTimeTestViewModel(ConnectionViewModel connectionViewModel,SettingsViewModel settingsViewModel)
        {
            ConnectionViewModel = connectionViewModel;
            SettingsViewModel = settingsViewModel;

            this.PropertyChanged += RealTimeTestViewModel_PropertyChanged;
            connectionViewModel.ParametersChanged += ConnectionViewModel_ParametersChanged;

            SignalTypes = new(Enum.GetValues<Parameter.DataChannelType>());
            SelectedSignalType = SignalTypes.FirstOrDefault();
            SetChannelType();

            WindowFunctions = new ObservableCollection<WindowFunction>(Enum.GetValues<WindowFunction>());
            SampleRateList = Parameter.GetSampleOptions();

            // 初始化数据保存服务
            _recorderService = new RecorderService(SettingsViewModel.DataStoragePath, new Progress<int>(progress => 
            {
                SaveProgress = progress;
                SaveStatus = $"保存进度： {progress}%";
            }));

            // 订阅数据采集完成事件
            _recorderService.DataCollectionCompleted += async (s, e) =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        _timer?.Stop();
                        ConnectionViewModel.SelectedDevice.DeviceControlApi.DataReceived -= DeviceControlApi_DataReceived;
                        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await ConnectionViewModel.SelectedDevice.DeviceControlApi.Stop(_cts.Token);
                        SelectedSignalTypeEnable = true;
                        IsTestRunning = false;
                        TriggerStatus = "准备就绪";
                        ShowSaveStatus = false;
                        // 停止数据记录
                        await _recorderService.StopRecordingAsync();
                        SaveStatus = "测试完成";
                        ReceivedPoints = SettingsViewModel.SelectedStorageType == StorageType.ByLength ? 
                            SettingsViewModel.DataLength.ToString() : 
                            $"{SettingsViewModel.StorageTime}秒";
                        SaveProgress = 100;
                    }
                    catch (Exception ex)
                    {
                        SaveStatus = $"停止测试失败: {ex.Message}";
                    }
                });
            };

            settingsViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.EnableDataStorage))
                {
                    ShowDataStorageInfo = settingsViewModel.EnableDataStorage && IsTestRunning;
                }
            };
        }

        private void ConnectionViewModel_ParametersChanged(object? sender, DeviceConnection e)
        {
            ControlEnable = ConnectionViewModel.SelectedDevice != null;

            if (ConnectionViewModel.SelectedDevice != null)
            {
                var parameter =
                    ConnectionViewModel.SelectedDevice.DeviceParameters.FirstOrDefault(d =>
                        d.Address == (int)ParameterType.SamplingRate);


                if (parameter != null)
                {
                    SelectedSampleRate = SampleRateList.FirstOrDefault(sr => (byte)sr.RealValue == (byte)parameter.Value);
                    SampleRate = Parameter.GetSampleRate((byte)SelectedSampleRate.RealValue);
                }


                parameter =
                    ConnectionViewModel.SelectedDevice.DeviceParameters.FirstOrDefault(d =>
                        d.Address == (int)ParameterType.UploadDataType);

                if (parameter != null)
                    SelectedSignalType =
                        SignalTypes.FirstOrDefault(sf => sf == (Parameter.DataChannelType)parameter.Value);

            }
        }



        private void RealTimeTestViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedSignalType))
            {
                SetChannelType();
            }
        }

        public void SetChannelType()
        {

            switch (SelectedSignalType)
            {
                case Parameter.DataChannelType.Velocity:
                case Parameter.DataChannelType.Displacement:
                case Parameter.DataChannelType.Acceleration:
                {
                    var waveform = new Waveform
                    {
                        DataChannelType = SelectedSignalType,
                        TimeDomainModel = new CuPlotModel(),
                        FrequencyDomainModel = new CuPlotModel
                        { Title = $"{SelectedSignalType} - 频域波形", IsLegendVisible = true }
                    };

                    waveform.TimeDomainModel.Title = $"{SelectedSignalType} - 时域波形";
                    waveform.TimeDomainModel.XTitle = "时间";
                    waveform.TimeDomainModel.XUint = "s";
                    waveform.TimeDomainModel.YTitle = SelectedSignalType.ToString();
                    waveform.TimeDomainModel.YUnit = "mm/s";

                    waveform.FrequencyDomainModel.Title = $"{SelectedSignalType} - 频域波形";
                    waveform.FrequencyDomainModel.XTitle = "频率";
                    waveform.FrequencyDomainModel.XUint = "Hz";
                    waveform.FrequencyDomainModel.YTitle = SelectedSignalType.ToString();
                    waveform.FrequencyDomainModel.YUnit = "mm/s";

                    waveform.TimeDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Left,
                        MaximumPadding = 0.1,
                        MinimumPadding = 0.1,

                    });
                    waveform.TimeDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Bottom,
                    });


                    waveform.FrequencyDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Left,
                        MaximumPadding = 0.1,
                        MinimumPadding = 0.1,
                    });
                    waveform.FrequencyDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Bottom,
                    });
                    var timeSeries = new LineSeries { Title = SelectedSignalType.ToString() };

                    timeSeries.Decimator = Decimator.Decimate;
                    waveform.TimeDomainModel.Series.Add(timeSeries);

                    var frequencySeries = new LineSeries { Title = SelectedSignalType.ToString() };

                    waveform.FrequencyDomainModel.Series.Add(frequencySeries);

                    Waveforms = new ObservableCollection<Waveform>(new[] { waveform });


                    waveform.TimeDomainModel.ApplyTheme();
                    waveform.FrequencyDomainModel.ApplyTheme();
                    waveform.InvalidatePlot(true);
                }
                    break;
                case Parameter.DataChannelType.ISignalAndQSignal:
                {
                    var waveform = new Waveform
                    {
                        DataChannelType = SelectedSignalType,
                        TimeDomainModel = new CuPlotModel(),
                        FrequencyDomainModel = new CuPlotModel()
                    };

                    waveform.TimeDomainModel.Title = $"{SelectedSignalType} - 时域波形";
                    waveform.TimeDomainModel.XTitle = "时间";
                    waveform.TimeDomainModel.XUint = "s";
                    waveform.TimeDomainModel.YTitle = SelectedSignalType.ToString();
                    waveform.TimeDomainModel.YUnit = "mm/s";

                    waveform.FrequencyDomainModel.Title = $"{SelectedSignalType} - 频域波形";
                    waveform.FrequencyDomainModel.XTitle = "频率";
                    waveform.FrequencyDomainModel.XUint = "Hz";
                    waveform.FrequencyDomainModel.YTitle = SelectedSignalType.ToString();
                    waveform.FrequencyDomainModel.YUnit = "mm/s";

       
                    waveform.TimeDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Left,
                        MaximumPadding = 0.1,
                        MinimumPadding = 0.1,

                    });
                    waveform.TimeDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Bottom,
  
                    });

                    waveform.FrequencyDomainModel.Legends.Add(new Legend());
                    waveform.FrequencyDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Left,
                        MaximumPadding = 0.1,
                        MinimumPadding = 0.1,
                    });
                    waveform.FrequencyDomainModel.Axes.Add(new LinearAxis()
                    {
                        Position = AxisPosition.Bottom,
              
                    });

                    var timeSeriesI = new LineSeries { Title = "I路" };
                    timeSeriesI.Decimator = Decimator.Decimate;
                    waveform.TimeDomainModel.Series.Add(timeSeriesI);
                    var timeSeriesQ = new LineSeries { Title = "Q路" };
                    timeSeriesQ.Decimator = Decimator.Decimate;
                    waveform.TimeDomainModel.Series.Add(timeSeriesQ);
                    var frequencySeriesI = new LineSeries { Title = "I路" };
                    waveform.FrequencyDomainModel.Series.Add(frequencySeriesI);
                    var frequencySeriesQ = new LineSeries { Title = "Q路" };
                    waveform.FrequencyDomainModel.Series.Add(frequencySeriesQ);
                    Waveforms = new ObservableCollection<Waveform>(new[] { waveform });

                    waveform.TimeDomainModel.ApplyTheme();
                    waveform.FrequencyDomainModel.ApplyTheme();

                    waveform.InvalidatePlot(true);
                }
                    break;

            }

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (ConnectionViewModel.SelectedDevice?.DeviceControlApi != null)
                _ = ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(new List<Parameter>()
                {
                    new()
                    {
                        Address = ParameterType.UploadDataType, Length = sizeof(Parameter.DataChannelType),
                        Value = SelectedSignalType
                    },
                }, _cts.Token);



        }



        // 波形数据类
        public class Waveform
        {
            public Parameter.DataChannelType DataChannelType { get; set; }
            public CuPlotModel TimeDomainModel { get; set; }
            public CuPlotModel FrequencyDomainModel { get; set; }

            public void InvalidatePlot(bool t)
            {
                TimeDomainModel.InvalidatePlot(t);
                FrequencyDomainModel.InvalidatePlot(t);
            }
        }

        // 开始测试命令
        public ICommand StartTestCommand => new RelayCommand(async () =>
        {
            if (ConnectionViewModel.SelectedDevice != null)
            {
                try
                {
                    IsTestRunning = true;
                    SaveStatus = "测试运行中";
                    ReceivedPoints = "0";
                    SaveProgress = -1;
                    TriggerStatus = SettingsViewModel.SelectedTriggerType == TriggerType.DebugTrigger ? "等待触发中..." : "准备就绪";


                    SelectedSignalTypeEnable = false;
                    SampleRate = Parameter.GetSampleRate((byte)SelectedSampleRate.RealValue);

                    _concurrentQueue = new ConcurrentQueue<List<double[]>>();
                    _cacheDatas = new List<List<double>>();

                    // 启动数据记录
                    if (SettingsViewModel.EnableDataStorage)
                    {
                        ShowSaveStatus = true;
                        _recorderService.StoragePath = SettingsViewModel.DataStoragePath;

                        await _recorderService.StartRecordingAsync(
                            SelectedSignalType,
                            SettingsViewModel.SelectedTriggerType,
                            SampleRate,
                            SettingsViewModel.DataLength,
                            SettingsViewModel.StorageTime,
                            SettingsViewModel.SelectedStorageType
                        );
                    }

                    foreach (var waveform in Waveforms)
                    {
                        foreach (var series in waveform.TimeDomainModel.Series)
                        {
                            if (series is LineSeries lineSeries)
                                lineSeries.Points.Clear();
                        }

                        foreach (var series in waveform.FrequencyDomainModel.Series)
                        {
                            if (series is LineSeries lineSeries)
                                lineSeries.Points.Clear();
                        }

                        waveform.InvalidatePlot(true);
                    }


                    ConnectionViewModel.SelectedDevice.DeviceControlApi.DataReceived += DeviceControlApi_DataReceived;

                    _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    //设置上传数据类型
                    await ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(
                        [
                            new()
                            {
                                Address = ParameterType.UploadDataType, Length = sizeof(Parameter.DataChannelType),
                                Value = SelectedSignalType
                            }
                        ],
                        _cts.Token);
                    _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    //设置采样率
                    await ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(
                        [
                            new()
                            {
                                Address = ParameterType.SamplingRate, Length = sizeof(byte),
                                Value = SelectedSampleRate.RealValue
                            },
                            //增加采样率的设置
                            new()
                            {
                                Address = ParameterType.LowPassFilter, Length = sizeof(byte),
                                Value = SelectedSampleRate.RealValue
                            }
                        ],
                        _cts.Token);



                    //设置触发类型
                    _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(
                        [
                            new()
                            {
                                Address = ParameterType.TriggerSampleType, Length = sizeof(byte),
                                Value = SettingsViewModel.SelectedTriggerType
                            },
       
                        ],
                        _cts.Token);


                    _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await ConnectionViewModel.SelectedDevice.DeviceControlApi.Start(_cts.Token);

                    _timer = new DispatcherTimer();
                    _timer.Tick += _timer_Tick;
                    _timer.Interval = TimeSpan.FromMilliseconds(30);
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    SaveStatus = $"启动测试失败: {ex.Message}";
                    IsTestRunning = false;
                }
            }
        });

        private void _timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            Task.Factory.StartNew(() =>
            {
                List<double[]> channelDatas = null;
                while (_concurrentQueue.TryDequeue(out var tmpChannelDatas))
                {
                    channelDatas = tmpChannelDatas;
                }

                if (channelDatas != null)
                {

                    var waveform = Waveforms.FirstOrDefault(w => w.DataChannelType == SelectedSignalType);
                    for (var i = 0; i < channelDatas.Count; i++)
                    {
                        var channelData = channelDatas[i];



                        if (RemoveDc)
                        {
                            var avg = channelData.Average();
                            for (var j = 0; j < channelData.Length; j++)
                            {
                                channelData[j] -= avg;

                            }
                        }



                        var timeDatas = new DataPoint[channelData.Length];
                        var freqDatas = new DataPoint[channelData.Length / 2];

                        if (!EnableFilter)
                        {
                            for (int j = 0; j < channelData.Length; j++)
                            {
                                timeDatas[j] = (new DataPoint(j / _sampleRate,
                                    channelData[j]));
                            }
                         
                        }
                        else
                        {
                            var tempData = (double[])channelData.Clone();
                            var filterChannelData = ProcessSamples(FilterType, tempData, SampleRate, LowPass, HighPass, BandPassFirst,
                                BandPassSecond, 0, 0);
                            for (int j = 0; j < filterChannelData.Length; j++)
                            {
                                timeDatas[j] = (new DataPoint(j / _sampleRate,
                                    filterChannelData[j]));
                            }

                        }




                        var windowFunction = SCSA.Utils.Window.GetWindow(SelectedWindowFunction, channelData.Length);

                        for (var j = 0; j < channelData.Length; j++)
                        {
                            channelData[j] *= windowFunction[j];
                        }
                        
                        var fftData = ComputeFFT(channelData);

                        var scale = _sampleRate / channelData.Length;
                        for (var j = 0; j < fftData.Length; j++)
                        {
                            freqDatas[j] = new DataPoint(j * scale, fftData[j]);
                        }

                        var minimum = double.NaN;
                        var maximum = double.NaN;

                        if (EnableFilter)
                        {

                            switch (FilterType)
                            {
                                case FilterType.LowPass:
                                    for (var j = 0; j < freqDatas.Length; j++)
                                    {
                                        if (freqDatas[j].X > LowPass)
                                        {
                                            freqDatas[j] = new DataPoint(freqDatas[j].X, 0);
                                        }
                                    }

                                    maximum = LowPass;
                                    break;
                                case FilterType.HighPass:
                                    for (var j = 0; j < freqDatas.Length; j++)
                                    {
                                        if (freqDatas[j].X < HighPass)
                                        {
                                            freqDatas[j] = new DataPoint(freqDatas[j].X, 0);
                                        }
                                    }

                                    minimum = HighPass;
                                    break;
                                case FilterType.BandPass:
                                    for (var j = 0; j < freqDatas.Length; j++)
                                    {
                                        if (freqDatas[j].X <= BandPassFirst || freqDatas[j].X >= BandPassSecond)
                                        {
                                            freqDatas[j] = new DataPoint(freqDatas[j].X, 0);
                                        }
                                    }

                                    minimum = BandPassFirst;
                                    maximum = BandPassSecond;
                                    break;

                            }
                        }

                        var timePeakResult = CalculateTimePeakResult(timeDatas);
                        var freqPeakResult = CalculateFreqPeakResult(freqDatas);
                        Dispatcher.UIThread.Invoke(() =>
                        {

                            var timeSeries = waveform.TimeDomainModel.Series[i] as LineSeries;
                            lock (waveform.TimeDomainModel.SyncRoot)
                            {
                                waveform.TimeDomainModel.SubTitle =
                                    $"平均值：{timePeakResult.AveragePeak:0.#####} 均方根值：{timePeakResult.EffectivePeak:0.#####} 峰峰值：{timePeakResult.PeakToPeak:0.#####}";
                                timeSeries.ItemsSource = timeDatas;
                                waveform.TimeDomainModel.InvalidatePlot(true);
                            }

                            var freqSeries = waveform.FrequencyDomainModel.Series[i] as LineSeries;
                            var xAxis = waveform.FrequencyDomainModel.Axes.First(ax =>
                                ax.Position == AxisPosition.Bottom);
                            lock (waveform.FrequencyDomainModel.SyncRoot)
                            {
                                waveform.FrequencyDomainModel.SubTitle =
                                    $"频率：{freqPeakResult.Position:0.#####} 幅值：{freqPeakResult.Peak:0.#####}";
                                freqSeries.ItemsSource = freqDatas;
                                xAxis.Minimum = minimum;
                                xAxis.Maximum = maximum;
                                waveform.FrequencyDomainModel.InvalidatePlot(true);
                            }
                        });
                    }

                }

                _timer.Start();
            });
        }

 



        // 停止测试命令
        public ICommand StopTestCommand => new RelayCommand(async () =>
        {
            if (ConnectionViewModel.SelectedDevice != null)
            {
                try
                {
                    _timer?.Stop();
                    ConnectionViewModel.SelectedDevice.DeviceControlApi.DataReceived -= DeviceControlApi_DataReceived;
                    _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await ConnectionViewModel.SelectedDevice.DeviceControlApi.Stop(_cts.Token);
                    SelectedSignalTypeEnable = true;
                    IsTestRunning = false;
                    TriggerStatus = "准备就绪";
                    ShowSaveStatus = false;
                    // 停止数据记录
                    if (SettingsViewModel.EnableDataStorage)
                    {
                        await _recorderService.StopRecordingAsync();
                        SaveStatus = "测试已停止";
                    }
                    else
                    {
                        SaveStatus = "测试已停止";
                    }
                }
                catch (Exception ex)
                {
                    SaveStatus = $"停止测试失败: {ex.Message}";
                }
            }
        });

        private async void DeviceControlApi_DataReceived(Dictionary<Parameter.DataChannelType, double[,]> channelDatas)
        {
            if (channelDatas.TryGetValue(SelectedSignalType, out var channelData))
            {
                // 直接保存原始数据
                if (IsTestRunning && SettingsViewModel.EnableDataStorage)
                {
                    try
                    {
                        _recorderService.WriteDataAsync(channelDatas);
                        
                        if (SettingsViewModel.SelectedStorageType == StorageType.ByLength)
                        {
                            ReceivedPoints = _recorderService.GetEnTotalDataPoints().ToString();
                        }
                        else
                        {
                            var points = _recorderService.GetEnTotalDataPoints();
                            var time = points / SampleRate;
                            ReceivedPoints = $"{time:F1}秒";
                        }
                        
                        if (SettingsViewModel.SelectedStorageType == StorageType.ByLength)
                        {
                            SaveProgress = (int)((double)_recorderService.GetTotalDataPoints() / SettingsViewModel.DataLength * 100);
                        }
                        else
                        {
                            var points = _recorderService.GetTotalDataPoints();
                            var totalPoints = (int)(SampleRate * SettingsViewModel.StorageTime);
                            SaveProgress = (int)((double)points / totalPoints * 100);
                        }
                    }
                    catch (Exception ex)
                    {
                        SaveStatus = $"保存数据失败: {ex.Message}";
                    }
                }
                else if (IsTestRunning)
                {
                    SaveProgress = -1;
                }

                // 处理显示数据
                while (_cacheDatas.Count < channelData.GetLength(0))
                {
                    _cacheDatas.Add(new List<double>());
                }

                for (int i = 0; i < channelData.GetLength(0); i++)
                {
                    _cacheDatas[i].AddRange(channelData.GetRow(i));
                }

                if (_cacheDatas[0].Count >= DisplayPointCount)
                {
                    var resultData = new List<double[]>();

                    for (var i = 0; i < _cacheDatas.Count; i++)
                    {
                        resultData.Add(_cacheDatas[i].Take(DisplayPointCount).ToArray());
                        _cacheDatas[i].RemoveRange(0, DisplayPointCount);
                    }

                    _concurrentQueue.Enqueue(resultData);
                }

                // 更新触发状态
                if (SettingsViewModel.SelectedTriggerType == TriggerType.DebugTrigger)
                {
                    TriggerStatus = "数据采集中...";
                }
            }
        }

        private double[] ComputeFFT(double[] d)
        {
            var data = d.Select(dd => new Complex(dd, 0)).ToArray();

            Fourier.Forward(data, FourierOptions.Matlab);

            var size = data.Length / 2;
            return data.Take(size).Select(dd => dd.Magnitude / size).ToArray();
        }
        public static double[] ProcessSamples(FilterType type, double[] inData, double sampleRate, double lowpass = 0,
            double highpass = 0, double bandpassFirst = 0, double bandpassSecond = 0, double BandstopFirst = 0, double BandstopSecond = 0)
        {
            var fftData = new Complex[inData.Length];
            for (var i = 0; i < fftData.Length; i++)
            {
                fftData[i] = new Complex(inData[i], 0);
            }

   

            Fourier.Forward(fftData, FourierOptions.Matlab);
            fftData = ApplyFilter(fftData,type, sampleRate, lowpass, highpass, bandpassFirst, bandpassSecond, BandstopFirst, BandstopSecond);
            Fourier.Inverse(fftData, FourierOptions.Matlab);

            var timeData = new double[fftData.Length];
            for (int i = 0; i < fftData.Length; i++)
            {
                timeData[i] = fftData[i].Real;
            }
            return timeData;
        }
        public static Complex[] ApplyFilter( Complex[] data, FilterType filterType, double sampleRate,
            double lowPass, double highPass, double bandPassFirstPass, double bandPassSecondPass, double bandStopFirst = 0, double bandStopSecond = 0)
        {
            Complex[] outData = data;

            switch (filterType)
            {
                case FilterType.LowPass:
                    OnlineFilter.Filter(filterType, data, out outData, sampleRate, lowPass);
                    break;
                case FilterType.HighPass:
                    OnlineFilter.Filter(filterType, data, out outData, sampleRate, highPass);
                    break;
                case FilterType.BandPass:
                    OnlineFilter.Filter(filterType, data, out outData, sampleRate, bandPassFirstPass,
                        bandPassSecondPass);
                    break;
                case FilterType.BandStop:
                    OnlineFilter.Filter(filterType, data, out outData, sampleRate, bandPassFirstPass,
                        bandPassSecondPass);
                    break;
                //case FilterType.AdvancedFilter:
                //    break;
            }

            return outData;
        }

        public static TimePeakResult CalculateTimePeakResult(DataPoint[] sourceData)
        {
            var inData = sourceData.Select(sd => sd.Y).ToArray();
            var peakResult = new TimePeakResult();
            double sum01 = 0.0, sum02 = 0.0;
            peakResult.MaxPeak = inData[0];
            peakResult.MinPeak = inData[0];
            for (int i = 0; i < inData.Length; ++i)
            {
                sum01 += inData[i];
                sum02 += inData[i] * inData[i];
                if (peakResult.MaxPeak < inData[i])
                {
                    peakResult.MaxPeak = inData[i];
                }

                if (peakResult.MinPeak > inData[i])
                {
                    peakResult.MinPeak = inData[i];
                }
            }

            peakResult.PeakToPeak = peakResult.MaxPeak - peakResult.MinPeak;

            peakResult.AveragePeak = sum01 / inData.Length;
            peakResult.EffectivePeak = Math.Sqrt(sum02 / inData.Length);

            return peakResult;
        }

        public static FreqPeakResult CalculateFreqPeakResult(DataPoint[] data)
        {
            var dp = data.OrderByDescending(d => d.Y).First();
            return new FreqPeakResult() { Position = dp.X,Peak = dp.Y };
        }

        partial void OnIsTestRunningChanged(bool value)
        {
            ShowDataStorageInfo = value && SettingsViewModel.EnableDataStorage;
        }
    }
}
