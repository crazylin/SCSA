using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using SCSA.Models;
using SCSA.Services;
using SCSA.Services.Recording;
using SCSA.Utils;
using SCSA.ViewModels.Messages;
using System.Runtime.CompilerServices;
using SCSA.Plot;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;

namespace SCSA.ViewModels;

public class RealTimeTestViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly object _cacheLock = new();
    private readonly ConnectionViewModel _connectionVm;
    private readonly IRecorderService _recorderService;
    private readonly StatusBarViewModel _statusBar;
    private List<CircularBuffer<double>> _cacheBuffers;

    private long _receivedDataPoints;
    private long _targetDataLength;
    private CancellationTokenSource _cts;
    private DeviceConnection? _currentDevice;
    private AppSettings _currentSettings;
    private DispatcherTimer _timer;
    private DispatcherTimer _runTimeTimer;
    private Stopwatch _stopwatch;
    // 标记后台处理是否正在进行，避免重入
    private volatile bool _isProcessing;

    // 标记是否已执行过停止清理，避免重复
    private int _hasCleaned;

    public RealTimeTestViewModel(IRecorderService recorderService, IAppSettingsService appSettingsService,
        ConnectionViewModel connectionViewModel, StatusBarViewModel statusBar)
    {
        try
        {
            // Load initial settings
            _currentSettings = appSettingsService.Load();
        }
        catch (Exception e)
        {
            Log.Error("加载应用设置失败", e);
            _currentSettings = new AppSettings(); // 使用默认设置
        }

        _recorderService = recorderService;
        _connectionVm = connectionViewModel;
        _statusBar = statusBar;
    

        //this.WhenActivated((CompositeDisposable disposables) =>
        //{
        //    // 当视图激活时，同步设备状态
        //    if (_connectionVm.SelectedDevice != null && _currentDevice != _connectionVm.SelectedDevice)
        //    {
        //        OnSelectedDeviceChanged(new SelectedDeviceChangedMessage(_connectionVm.SelectedDevice));
        //    }
        //    else if (_connectionVm.SelectedDevice == null && _currentDevice != null)
        //    {
        //        OnSelectedDeviceChanged(new SelectedDeviceChangedMessage(null));
        //    }
        //});

        // Message Bus Subscriptions
        MessageBus.Current.Listen<SelectedDeviceChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSelectedDeviceChanged);

        MessageBus.Current.Listen<ParametersChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnParametersChanged);

        MessageBus.Current.Listen<SettingsChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSettingsChanged);

        _recorderService.DataCollectionCompleted += OnDataCollectionCompletedAsync;

        // Command Definitions
        var canStart = this.WhenAnyValue(x => x.ControlEnable, x => x.IsTestRunning,
            (enable, isRunning) => enable && !isRunning);

        StartTestCommand = ReactiveCommand.CreateFromTask(StartTestAsync, canStart);
        StopTestCommand = ReactiveCommand.CreateFromTask(StopTestAsync, this.WhenAnyValue(x => x.IsTestRunning));

        ToggleTestCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (IsTestRunning)
                await StopTestAsync();
            else
                await StartTestAsync();
        });

        // Exception Handling
        StartTestCommand.ThrownExceptions.Subscribe(ex =>
        {
            Log.Error("启动测试命令执行失败", ex);
            TestStatus = $"启动测试失败: {ex.Message}";
        });
        StopTestCommand.ThrownExceptions.Subscribe(ex =>
        {
            Log.Error("停止测试命令执行失败", ex);
            TestStatus = $"停止测试失败: {ex.Message}";
        });


        SelectedTriggerType = _currentSettings.SelectedTriggerType;

        SignalTypes = new ObservableCollection<Parameter.DataChannelType>(Enum.GetValues<Parameter.DataChannelType>());
        SelectedSignalType = SignalTypes.FirstOrDefault();
        SetChannelType();

        WindowFunctions = new ObservableCollection<WindowFunction>(Enum.GetValues<WindowFunction>());
        SampleRateList = Parameter.GetSampleOptions();

        // Property Change Subscriptions
        this.WhenAnyValue(x => x.SelectedSignalType)
            .Skip(1)
            .Subscribe(_ => SetChannelType());

        this.WhenAnyValue(x => x.SelectedSampleRate)
            .Skip(1)
            .Subscribe(sr =>
            {
                if (sr != null) SampleRate = Parameter.GetSampleRate((byte)sr.RealValue);
            });

        this.WhenAnyValue(x => x.IsTestRunning)
            .Subscribe(running =>
            {
                ShowDataStorageInfo = _currentSettings.EnableDataStorage && running;
                if (_statusBar != null) _statusBar.ShowDataStorageInfo = ShowDataStorageInfo;
            });

        this.WhenAnyValue(x => x.TriggerStatus)
            .Subscribe(status =>
            {
                if (_statusBar != null) _statusBar.TriggerStatus = status;
            });
    }

    public ViewModelActivator Activator { get; } = new();


    private void OnSettingsChanged(SettingsChangedMessage msg)
    {
        _currentSettings = msg.Value;
        SelectedTriggerType = _currentSettings.SelectedTriggerType;
    }

    private void OnParametersChanged(ParametersChangedMessage msg)
    {
        if (_currentDevice != null && _currentDevice.EndPoint.Equals(msg.Value.EndPoint))
            UpdateParameters(msg.Value.DeviceParameters);
    }

    private void OnSelectedDeviceChanged(SelectedDeviceChangedMessage msg)
    {
        _currentDevice = msg.Value;
        ControlEnable = _currentDevice != null;
        if (_currentDevice?.DeviceParameters != null)
        {
            UpdateParameters(_currentDevice.DeviceParameters);
        }
        else
        {
            // 如果设备为空，也需要清空参数相关的UI
            SelectedSampleRate = null;
            // 可以根据需要清空其他依赖于设备的属性
        }
    }

    private void UpdateParameters(List<DeviceParameter> parameters)
    {
        if (parameters == null) return;
        var samplingRateParam = parameters.FirstOrDefault(p => p.Address == (int)ParameterType.SamplingRate);
        if (samplingRateParam != null)
            SelectedSampleRate = SampleRateList.FirstOrDefault(sr => sr.RealValue.Equals(Convert.ToByte(samplingRateParam.Value)));
        var dataTypeParam = parameters.FirstOrDefault(p => p.Address == (int)ParameterType.UploadDataType);
        if (dataTypeParam != null) 
            SelectedSignalType = (Parameter.DataChannelType)Convert.ToInt32(dataTypeParam.Value);
    }

    private async void OnDataCollectionCompletedAsync(object? sender, EventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await CleanupAfterTestAsync("测试完成");
        });
    }


    public void SetChannelType()
    {
        Waveforms = new ObservableCollection<Waveform>();
        switch (SelectedSignalType)
        {
            case Parameter.DataChannelType.Velocity:
                Waveforms.Add(CreateWaveform(SelectedSignalType, "mm/s", new[] { SelectedSignalType.ToString() }));
                break;
            case Parameter.DataChannelType.Displacement:
                Waveforms.Add(CreateWaveform(SelectedSignalType, "µm", new[] { SelectedSignalType.ToString() }));
                break;
            case Parameter.DataChannelType.Acceleration:
                Waveforms.Add(CreateWaveform(SelectedSignalType, "m/s\u00b2", new[] { SelectedSignalType.ToString() }));
                break;
            case Parameter.DataChannelType.ISignalAndQSignal:
                Waveforms.Add(CreateWaveform(SelectedSignalType, "V", new[] { "I Signal", "Q Signal" }));
                break;
            default:
                Waveforms.Add(CreateWaveform(SelectedSignalType, "V", new[] { SelectedSignalType.ToString() }));
                break;
        }
    }

    private Waveform CreateWaveform(Parameter.DataChannelType channelType, string yUnit, string[] seriesTitles)
    {
        var waveform = new Waveform { DataChannelType = channelType };

        // Time Domain Plot
        waveform.TimeDomainModel = new CuPlotViewModel()
        {
            PlotModel = new CuPlotModel
            {
                Title = $"{channelType} - 时域波形",
                XTitle = "时间",
                XUint = "s",
                YTitle = channelType.ToString(),
                YUnit = yUnit
            },

        };
        waveform.TimeDomainModel.PlotModel.Axes.Add(new LinearAxis
            { Position = AxisPosition.Left, MaximumPadding = 0.1, MinimumPadding = 0.1 });
        waveform.TimeDomainModel.PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });

        // Frequency Domain Plot
        waveform.FrequencyDomainModel = new CuPlotViewModel()
        {
            PlotModel = new CuPlotModel
            {
                Title = $"{channelType} - 频域波形",
                IsLegendVisible = true,
                XTitle = "频率",
                XUint = "Hz",
                YTitle = channelType.ToString(),
                YUnit = yUnit
            }
        };
        waveform.FrequencyDomainModel.PlotModel.Axes.Add(new LinearAxis
            { Position = AxisPosition.Left, MaximumPadding = 0.1, MinimumPadding = 0.1 });
        waveform.FrequencyDomainModel.PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });

        foreach (var title in seriesTitles)
        {
            waveform.TimeDomainModel.PlotModel.Series.Add(new LineSeries { Title = title, Decimator = Decimator.Decimate});
            waveform.FrequencyDomainModel.PlotModel.Series.Add(new LineSeries { Title = title });
        }

        waveform.TimeDomainModel.PlotModel.ApplyTheme();
        waveform.FrequencyDomainModel.PlotModel.ApplyTheme();

        // 若为 IQ 信号，创建李萨如图
        if (channelType == Parameter.DataChannelType.ISignalAndQSignal)
        {
            waveform.LissajousModel = new CuPlotViewModel()
            {
                PlotModel = new CuPlotModel(false)
                {
                    Title = "I-Q 李萨如图",
                    XTitle = "I",
                    YTitle = "Q",
                    IsLegendVisible = false
                }
            };
            waveform.LissajousModel.PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                MaximumPadding = 0.1,
                MinimumPadding = 0.1
            });
            waveform.LissajousModel.PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });

            waveform.LissajousModel.PlotModel.Series.Add(new ScatterSeries { Title = "IQ",MarkerSize = 1});
            waveform.LissajousModel.PlotModel.ApplyTheme();
        }

        waveform.InvalidatePlot(true);

        return waveform;
    }


    private async Task StartTestAsync()
    {
        if (_currentDevice == null)
        {
            TestStatus = "未选择设备";
            return;
        }

        // 重置清理标记
        _hasCleaned = 0;
        _receivedDataPoints = 0;
        ReceivedProgress = 0;
        try
        {
            IsTestRunning = true;
            _statusBar.IsTestRunning = true;
            SelectedSignalTypeEnable = false;
            _cacheBuffers = new List<SCSA.Utils.CircularBuffer<double>>();

            _statusBar.AcquisitionMode = $"采集模式: {SelectedSignalType}";
            _statusBar.TriggerMode = $"触发模式: {SelectedTriggerType}";
            _statusBar.ShowTriggerStatus = SelectedTriggerType != TriggerType.FreeTrigger;
            if (_currentSettings.EnableDataStorage)
            {
                var saveProgress = new Progress<int>(p =>
                {
                    SaveProgress = p;
                    _statusBar.SaveProgress = p;
                });
                _targetDataLength = await _recorderService.StartRecordingAsync(SelectedSignalType, _currentSettings.SelectedTriggerType,
                    SampleRate, saveProgress,
                    _currentSettings.DataLength, _currentSettings.StorageTime, _currentSettings.SelectedStorageType,
                    _currentSettings.SelectedFileFormat, _currentSettings.SelectedUFFFormat == UFFFormatType.Binary);
            }


            _currentDevice.DeviceControlApi.DataReceived += DeviceControlApi_DataReceived;


            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _currentDevice.DeviceControlApi.SetParameters(
                [
                    //上传数据类型
                    new Parameter
                    {
                        Address = ParameterType.UploadDataType, Length = sizeof(Parameter.DataChannelType),
                        Value = SelectedSignalType
                    },
                    //采样率
                    new Parameter
                    {
                        Address = ParameterType.SamplingRate, Length = sizeof(byte),
                        Value = SelectedSampleRate.RealValue
                    },
                    //输出滤波
                    new Parameter
                    {
                        Address = ParameterType.LowPassFilter, Length = sizeof(byte),
                        Value = SelectedSampleRate.RealValue
                    },
                    //触发类型
                    new Parameter
                    {
                        Address = ParameterType.TriggerSampleType, Length = sizeof(byte),
                        Value = _currentSettings.SelectedTriggerType
                    }
                ],
                _cts.Token);

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _currentDevice.DeviceControlApi.Start(_cts.Token);
            TestStatus = "测试进行中...";

            ShoudUpdate = true;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += _timer_Tick;
            _timer.Start();

            _stopwatch = Stopwatch.StartNew();
            _runTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _runTimeTimer.Tick += (s, e) => _statusBar.TestRunningTime = $"运行时: {_stopwatch.Elapsed:hh\\:mm\\:ss}";
            _runTimeTimer.Start();
        }
        catch (Exception e)
        {
            Log.Error("启动测试失败", e);
            TestStatus = $"启动测试失败: {e.Message}";

            IsTestRunning = false;
            SelectedSignalTypeEnable = true;
        }
    }

    private void _timer_Tick(object? sender, EventArgs e)
    {
        if (_isProcessing) return;

        // 快速从缓存抓取数据（UI 线程）
        List<double[]> latestData;
        lock (_cacheLock)
        {
            latestData = _cacheBuffers.Select(b => b.ToArray()).ToList();
        }

        if (latestData.Count == 0 || latestData[0].Length < DisplayPointCount)
        {
            return; // 数据不足，等待下一轮
        }

        _isProcessing = true;
        _timer.Stop();

        // 捕获当前状态，避免后台线程访问 UI 线程对象
        var sampleRate = SampleRate;
        var windowFunc = SelectedWindowFunction;
        var removeDc = RemoveDc;
        var enableFilter = EnableFilter;
        var filterType = FilterType;
        var lowPass = LowPass;
        var highPass = HighPass;
        var bandPassFirst = BandPassFirst;
        var bandPassSecond = BandPassSecond;
        var yUnit = Waveforms[0].FrequencyDomainModel.PlotModel.YUnit;
        var isIQ = SelectedSignalType == Parameter.DataChannelType.ISignalAndQSignal;

        Task.Run(() =>
        {
            try
            {
                // -----------------------------  后台线程：数据预处理  -----------------------------
                var processedData = new List<double[]>();      // 时域显示使用
                var fftInputData = new List<double[]>();       // 用于 FFT

                foreach (var ch in latestData)
                {
                    var data = new double[ch.Length];
                    Array.Copy(ch, data, ch.Length);

                    // 1) 去直流
                    if (removeDc)
                    {
                        var mean = data.Average();
                        for (int i = 0; i < data.Length; i++)
                            data[i] -= mean;
                    }

                    // 2) 加窗（FFT 使用）
                    var window = Window.GetWindow(windowFunc, data.Length);
                    var winData = new double[data.Length];
                    for (int i = 0; i < data.Length; i++)
                        winData[i] = data[i] * window[i];
                    fftInputData.Add(winData);

                    // 3) 滤波
                    if (enableFilter)
                    {
                        data = SignalProcessingService.FilterSamples(
                            filterType,
                            data,
                            sampleRate,
                            lowPass,
                            highPass,
                            bandPassFirst,
                            bandPassSecond);
                    }

                    processedData.Add(data);
                }

                // 生成时域 DataPoints
                var timeSeriesPoints = new List<List<DataPoint>>();
                for (int i = 0; i < processedData.Count; i++)
                {
                    var seriesPts = new List<DataPoint>(processedData[i].Length);
                    for (int j = 0; j < processedData[i].Length; j++)
                        seriesPts.Add(new DataPoint(j / sampleRate, processedData[i][j]));
                    timeSeriesPoints.Add(seriesPts);
                }

                // 计算时域峰值字符串
                string fmta(double d) => $"{d,9:+0.000;-0.000;0.000}";
                string fmt(double d) => $"{d,8:0.000}";
                var timePeakResults = processedData.Select(SignalProcessingService.CalculateTimePeak).ToList();
                var timeSubTitle = string.Join(" / ",
                    timePeakResults.Select(r =>
                        $"Avg:{fmta(r.AveragePeak)} Pk-Pk:{fmt(r.PeakToPeak)} RMS:{fmt(r.EffectivePeak)} ({yUnit})"));

                // -----------------------------  FFT  -----------------------------
                var fftData = fftInputData.Select(pd => SignalProcessingService.ComputeFft(pd)).ToList();

                var freqSeriesPoints = new List<List<DataPoint>>();
                for (int i = 0; i < fftData.Count; i++)
                {
                    var pts = new List<DataPoint>(fftData[i].Length);
                    var freqStep = sampleRate / processedData[i].Length;
                    for (int j = 0; j < fftData[i].Length; j++)
                        pts.Add(new DataPoint(j * freqStep, fftData[i][j]));
                    freqSeriesPoints.Add(pts);
                }

                // 计算频域峰值
                string fmtFreq(double d) => $"{d,8:0.000;0.000}";
                var freqPeakResults = freqSeriesPoints.Select((pts) =>
                    SignalProcessingService.CalculateFreqPeak(pts.ToArray())).ToList();
                var freqSubTitle = string.Join(" / ",
                    freqPeakResults.Select(r => $"F:{fmtFreq(r.Position)}Hz Pk:{fmtFreq(r.Peak)} ({yUnit})"));

                // 轴范围
                double nyquist = sampleRate / 2;
                double minFreq = 0;
                double maxFreq = nyquist;
                if (enableFilter)
                {
                    switch (filterType)
                    {
                        case FilterType.LowPass:
                            maxFreq = lowPass;
                            break;
                        case FilterType.HighPass:
                            minFreq = highPass;
                            break;
                        case FilterType.BandPass:
                            minFreq = bandPassFirst;
                            maxFreq = bandPassSecond;
                            break;
                    }
                    if (maxFreq <= 0) maxFreq = nyquist;
                    if (minFreq < 0) minFreq = 0;
                    if (maxFreq <= minFreq) maxFreq = minFreq + nyquist / 10;
                }

                // -----------------------------  IQ 李萨如图 -----------------------------
                List<ScatterPoint>? lissaPts = null;
                string? lissaSubTitle = null;
                double strength = 0;
                if (isIQ && processedData.Count >= 2)
                {
                    var iSig = processedData[0];
                    var qSig = processedData[1];
                    int lenSig = iSig.Length;
                    lissaPts = new List<ScatterPoint>(lenSig);
                    for (int k = 0; k < lenSig; k++)
                        lissaPts.Add(new ScatterPoint(iSig[k], qSig[k]));

                    // 相关系数
                    double sumI = 0, sumQ = 0, sumI2 = 0, sumQ2 = 0, sumIQ = 0;
                    for (int m = 0; m < lenSig; m++)
                    {
                        var i = iSig[m];
                        var q = qSig[m];
                        sumI += i;
                        sumQ += q;
                        sumI2 += i * i;
                        sumQ2 += q * q;
                        sumIQ += i * q;
                    }
                    double meanI = sumI / lenSig;
                    double meanQ = sumQ / lenSig;
                    double cov = (sumIQ / lenSig) - meanI * meanQ;
                    double stdI = Math.Sqrt(Math.Max((sumI2 / lenSig) - meanI * meanI, 1e-12));
                    double stdQ = Math.Sqrt(Math.Max((sumQ2 / lenSig) - meanQ * meanQ, 1e-12));
                    double den = Math.Sqrt((lenSig * sumI2 - sumI * sumI) * (lenSig * sumQ2 - sumQ * qSig.Sum()));
                    strength = den == 0 ? 0 : (lenSig * sumIQ - sumI * sumQ) / den;

                    double corr = cov / (stdI * stdQ);
                    // 信号强度
                    double strengthRms = 0;
                    for (int m = 0; m < lenSig; m++)
                        strengthRms += iSig[m] * iSig[m] + qSig[m] * qSig[m];
                    strengthRms = Math.Sqrt(strengthRms / lenSig);
                    strength = Math.Min(1.0, strengthRms / 100.0);

                    string fmt8(double d) => $"{d,8:0.000}";
                    lissaSubTitle = $"Corr:{fmt8(corr)}  RMS:{fmt8(strengthRms)}";
                }

                var result = new PlotData(timeSeriesPoints, timeSubTitle, freqSeriesPoints, freqSubTitle, lissaPts, lissaSubTitle, minFreq, maxFreq, strength);

                // ------------------ 切回 UI 线程进行渲染 ------------------
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ApplyPlotData(result);
                    _isProcessing = false;
                    _timer.Start();
                });
            }
            catch (Exception ex)
            {
                Log.Error("后台数据处理失败", ex);
            }
        });
    }

    private record PlotData(
        List<List<DataPoint>> TimeSeries,
        string TimeSubTitle,
        List<List<DataPoint>> FreqSeries,
        string FreqSubTitle,
        List<ScatterPoint>? Lissajous,
        string? LissajousSubTitle,
        double MinFreq,
        double MaxFreq,
        double Strength);

    private void ApplyPlotData(PlotData data)
    {
        // 更新时域
        var timeDomainSeries = Waveforms[0].TimeDomainModel.PlotModel.Series;
        for (int i = 0; i < data.TimeSeries.Count && i < timeDomainSeries.Count; i++)
        {
            var series = (LineSeries)timeDomainSeries[i];
            series.Points.Clear();
            series.Points.AddRange(data.TimeSeries[i]);
        }
        Waveforms[0].TimeDomainModel.PlotModel.SubTitle = data.TimeSubTitle;
        Waveforms[0].TimeDomainModel.PlotModel.InvalidatePlot(true);

        // 更新频域
        var freqDomainSeries = Waveforms[0].FrequencyDomainModel.PlotModel.Series;
        for (int i = 0; i < data.FreqSeries.Count && i < freqDomainSeries.Count; i++)
        {
            var series = (LineSeries)freqDomainSeries[i];
            series.Points.Clear();
            series.Points.AddRange(data.FreqSeries[i]);
        }
        Waveforms[0].FrequencyDomainModel.PlotModel.SubTitle = data.FreqSubTitle;
        var xAxis = Waveforms[0].FrequencyDomainModel.PlotModel.Axes.First(a => a.Position == AxisPosition.Bottom);
        xAxis.Minimum = data.MinFreq;
        xAxis.Maximum = data.MaxFreq;
        Waveforms[0].FrequencyDomainModel.PlotModel.InvalidatePlot(true);

        // 更新 Lissajous
        if (data.Lissajous != null && Waveforms[0].LissajousModel != null)
        {
            var lissajousSeries = Waveforms[0].LissajousModel.PlotModel.Series.FirstOrDefault() as ScatterSeries;
            if (lissajousSeries != null)
            {
                lissajousSeries.Points.Clear();
                lissajousSeries.Points.AddRange(data.Lissajous);
                Waveforms[0].LissajousModel.PlotModel.SubTitle = data.LissajousSubTitle ?? string.Empty;
                Waveforms[0].LissajousModel.PlotModel.InvalidatePlot(true);
                Waveforms[0].Strength = data.Strength;
            }
        }
    }

    private async Task StopTestAsync()
    {
        if (_currentDevice == null) return;

        try
        {
            // 停止设备
            _cts?.Cancel();
     
            // 停止录制
            await _recorderService.StopRecordingAsync();

            // 统一清理
            await CleanupAfterTestAsync("测试已停止");
        }
        catch (Exception e)
        {
            Log.Error("停止测试失败", e);
            TestStatus = $"停止测试失败:{e.Message}";

        }
    }

    /// <summary>
    /// 停止测试后的统一清理逻辑，确保最多执行一次。
    /// </summary>
    private async Task CleanupAfterTestAsync(string finalStatus)
    {
        if (Interlocked.Exchange(ref _hasCleaned, 1) == 1) return;

        _statusBar.IsTestRunning = false;
        _statusBar.ShowTriggerStatus = false;
        _runTimeTimer?.Stop();
        _stopwatch?.Stop();
        _statusBar.SaveProgress = 0;
        _statusBar.ReceivedProgress = 0;
        _targetDataLength = 0;
        _receivedDataPoints = 0;
        _statusBar.TestRunningTime = "运行时: 00:00:00";

        try
        {
            _timer?.Stop();

            if (_currentDevice?.DeviceControlApi != null)
            {
                _currentDevice.DeviceControlApi.DataReceived -= DeviceControlApi_DataReceived;
                try
                {
                    await _currentDevice.DeviceControlApi.Stop(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                }
                catch(Exception ex)
                {
                    Log.Error("请求设备停止失败", ex);
                    // 忽略停止设备时的异常
                }
            }

            SelectedSignalTypeEnable = true;
            IsTestRunning = false;
            ShoudUpdate = false;
            TriggerStatus = "准备就绪";

            TestStatus = finalStatus;

        }
        catch (Exception ex)
        {
            Log.Error("测试清理失败", ex);
            TestStatus = $"停止测试失败:{ex.Message}";

        }
    }

    private void DeviceControlApi_DataReceived(Dictionary<Parameter.DataChannelType, double[,]> channelDatas)
    {
        try
        {
            if (ShoudUpdate && channelDatas.TryGetValue(SelectedSignalType, out var rawData))
            {
                // 更新接收进度
                if (IsTestRunning && _targetDataLength > 0)
                {
                    TriggerStatus = "触发采集中...";
                    _receivedDataPoints += rawData.GetLength(1);
                    var progress = (int)((double)_receivedDataPoints / _targetDataLength * 100);
                    progress = Math.Min(progress, 100);
                    ReceivedProgress = progress;
                    _statusBar.ReceivedProgress = progress;
                    if (progress >= 100)
                    {
                        TriggerStatus = "采集完成";
                    }
                }
                
                // 根据信号类型决定缩放因子
                double factor = 1.0;
                switch (SelectedSignalType)
                {
                    case Parameter.DataChannelType.Velocity:
                        factor = SampleRate;
                        break;
                    case Parameter.DataChannelType.Acceleration:
                        factor = SampleRate * SampleRate;
                        break;
                }

                // 若需要缩放，复制并乘以因子；否则直接复用原数据
                double[,] dataForProcess;
                if (Math.Abs(factor - 1.0) > double.Epsilon)
                {
                    var rows = rawData.GetLength(0);
                    var cols = rawData.GetLength(1);
                    dataForProcess = new double[rows, cols];
                    Parallel.For(0, rows, r =>
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            dataForProcess[r, c] = rawData[r, c] * factor;
                        }
                    });
                }
                else
                {
                    dataForProcess = rawData;
                }

                // 构建新的 channelDatas 以确保写盘和后续处理使用相同缩放
                var scaledDict = new Dictionary<Parameter.DataChannelType, double[,]> { { SelectedSignalType, dataForProcess } };

                if (IsTestRunning && _currentSettings.EnableDataStorage) _recorderService.WriteDataAsync(scaledDict);

                lock (_cacheLock)
                {
                    var channelCount = dataForProcess.GetLength(0);
                    var sampleCount = dataForProcess.GetLength(1);

                    while (_cacheBuffers.Count < channelCount)
                        _cacheBuffers.Add(new CircularBuffer<double>(DisplayPointCount));

                    for (var i = 0; i < channelCount; i++)
                        for (var j = 0; j < sampleCount; j++)
                            _cacheBuffers[i].Add(dataForProcess[i, j]);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("处理接收到的数据时出错", e);
        }
    }


    // 波形数据类
    public class Waveform : ReactiveObject
    {
        public Parameter.DataChannelType DataChannelType { get; set; }
        public CuPlotViewModel TimeDomainModel { get; set; }
        public CuPlotViewModel FrequencyDomainModel { get; set; }
        public CuPlotViewModel LissajousModel { get; set; }
        
        [Reactive]public double Strength { set; get; }

        public void InvalidatePlot(bool t)
        {
            TimeDomainModel.PlotModel.InvalidatePlot(t);
            FrequencyDomainModel.PlotModel.InvalidatePlot(t);
        }
    }

    #region Properties

    [Reactive] public int DisplayPointCount { get; set; } = 10240;

    [Reactive] public Parameter.DataChannelType SelectedSignalType { get; set; }

    [Reactive] public ObservableCollection<Waveform> Waveforms { get; set; }

    [Reactive] public ObservableCollection<Parameter.DataChannelType> SignalTypes { get; set; }

    [Reactive] public ObservableCollection<WindowFunction> WindowFunctions { get; set; }

    [Reactive] public WindowFunction SelectedWindowFunction { get; set; }

    [Reactive] public bool RemoveDc { get; set; }

    [Reactive] public bool SelectedSignalTypeEnable { get; set; } = true;

    [Reactive] public double SampleRate { get; set; }

    [Reactive] public bool ControlEnable { get; set; }

    [Reactive] public List<EnumOption> SampleRateList { get; set; }

    [Reactive] public EnumOption SelectedSampleRate { get; set; }

    [Reactive] public bool EnableFilter { get; set; }

    [Reactive] public FilterType FilterType { get; set; } = FilterType.LowPass;

    [Reactive] public double LowPass { get; set; } = 500;

    [Reactive] public double HighPass { get; set; } = 500;

    [Reactive] public double BandPassFirst { get; set; } = 100;

    [Reactive] public double BandPassSecond { get; set; } = 500;

    [Reactive] public double SaveProgress { get; set; }

    [Reactive] public string TestStatus { get; set; } = "";

    [Reactive] public bool IsTestRunning { get; set; }

    [Reactive] public double ReceivedProgress { get; set; }

    [Reactive] public bool ShowDataStorageInfo { get; set; }

    [Reactive] public string TriggerStatus { get; set; } = "准备就绪";

    [Reactive] public bool ShoudUpdate { get; set; }

    [Reactive] public TriggerType SelectedTriggerType { get; private set; }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> StopTestCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleTestCommand { get; }

    #endregion

}