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

namespace SCSA.ViewModels;

public class RealTimeTestViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly object _cacheLock = new();
    private readonly ConnectionViewModel _connectionVm;
    private readonly IRecorderService _recorderService;
    private List<CircularBuffer<double>> _cacheBuffers;

    private CancellationTokenSource _cts;
    private DeviceConnection? _currentDevice;
    private AppSettings _currentSettings;
    private DispatcherTimer _timer;

    // 标记是否已执行过停止清理，避免重复
    private int _hasCleaned;

    public RealTimeTestViewModel(IRecorderService recorderService, IAppSettingsService appSettingsService,
        ConnectionViewModel connectionViewModel)
    {

        // Load initial settings
        _currentSettings = appSettingsService.Load();

        _recorderService = recorderService;
        _connectionVm = connectionViewModel;
    

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
        StopTestCommand =
            ReactiveCommand.CreateFromTask(StopTestAsync, this.WhenAnyValue(x => x.IsTestRunning));

        // Exception Handling
        StartTestCommand.ThrownExceptions.Subscribe(ex => TestStatus = $"启动测试失败: {ex.Message}");
        StopTestCommand.ThrownExceptions.Subscribe(ex => TestStatus = $"停止测试失败: {ex.Message}");


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
            .Subscribe(sr => SampleRate = Parameter.GetSampleRate((byte)sr.RealValue));

        this.WhenAnyValue(x => x.IsTestRunning)
            .Subscribe(running => ShowDataStorageInfo = _currentSettings.EnableDataStorage && running);

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
        if (_currentDevice?.DeviceParameters != null) UpdateParameters(_currentDevice.DeviceParameters);
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

        try
        {
            IsTestRunning = true;
            SelectedSignalTypeEnable = false;
            _cacheBuffers = new List<SCSA.Utils.CircularBuffer<double>>();

            if (_currentSettings.EnableDataStorage)
            {
                var saveProgress = new Progress<int>(p => SaveProgress = p);
                var receivedProgress = new Progress<int>(p =>
                {
                    ReceivedProgress = p;TriggerStatus = "触发采集中...";
                });
                await _recorderService.StartRecordingAsync(SelectedSignalType, _currentSettings.SelectedTriggerType,
                    SampleRate, saveProgress, receivedProgress,
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
            StatusColor = new SolidColorBrush(Colors.Green);
            ShoudUpdate = true;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }
        catch (Exception e)
        {
            TestStatus = $"启动测试失败: {e.Message}";
            StatusColor = new SolidColorBrush(Colors.Red);
            IsTestRunning = false;
            SelectedSignalTypeEnable = true;
        }
    }

    private void _timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        List<double[]> latestData;
        lock (_cacheLock)
        {
            // Snapshot
            latestData = _cacheBuffers.Select(buf => buf.ToArray()).ToList();
        }

        if (latestData.Count == 0)
        {
            _timer.Start();
            return;
        };

        // 若采样数量不足，不刷新，避免频繁重绘
        if (latestData[0].Length < DisplayPointCount)
        {
            _timer.Start();
            return;
        }

        // ---------------------
        // 预处理：去直流 + 滤波 + 加窗
        // ---------------------

        var processedData = new List<double[]>();      // 时域显示使用
        var fftInputData = new List<double[]>();       // 仅用于FFT，额外乘以窗函数

        foreach (var ch in latestData)
        {
            var data = new double[ch.Length];
            Array.Copy(ch, data, ch.Length);

            // 1) 去直流
            if (RemoveDc)
            {
                var mean = data.Average();
                for (var i = 0; i < data.Length; i++)
                    data[i] -= mean;
            }
   
            // 2) 加窗用于FFT
            var window = Window.GetWindow(SelectedWindowFunction, data.Length);
            var winData = new double[data.Length];
            for (var i = 0; i < data.Length; i++)
                winData[i] = data[i] * window[i];
            fftInputData.Add(winData);

            // 3) 滤波
            if (EnableFilter)
                data = SignalProcessingService.FilterSamples(
                    FilterType,
                    data,
                    SampleRate,
                    LowPass,
                    HighPass,
                    BandPassFirst,
                    BandPassSecond);

            // 保存时域数据（无窗）
            processedData.Add(data);

        }

        // 计算时域峰值（根据所有通道，使用" / "分隔）
        if (processedData.Count > 0)
        {
            var timePeakResults = processedData.Select(SignalProcessingService.CalculateTimePeak).ToList();
            string fmta(double d) => $"{d,9:+0.000;-0.000;0.000}"; // 始终带符号并固定 9 宽
            string fmt(double d) => $"{d,8:0.000}"; // 始终带符号并固定 8 宽
            var channelStrings = timePeakResults
                .Select(r => $"Avg:{fmta(r.AveragePeak)} Pk-Pk:{fmt(r.PeakToPeak)} RMS:{fmt(r.EffectivePeak)} ({Waveforms[0].FrequencyDomainModel.PlotModel.YUnit})");
            Waveforms[0].TimeDomainModel.PlotModel.SubTitle = string.Join(" / ", channelStrings);
        }

        // Update time-domain plot (使用处理后的数据)
        var timeDomainSeries = Waveforms[0].TimeDomainModel.PlotModel.Series;
        for (var i = 0; i < processedData.Count && i < timeDomainSeries.Count; i++)
        {
            var series = (LineSeries)timeDomainSeries[i];
            var points = new List<DataPoint>(processedData[i].Length);
            for (var j = 0; j < processedData[i].Length; j++)
                points.Add(new DataPoint(j / SampleRate, processedData[i][j]));
            series.Points.Clear();
            series.Points.AddRange(points);
        }
     
        Waveforms[0].TimeDomainModel.PlotModel.InvalidatePlot(true);

        // 计算 FFT
        var fftData = fftInputData.Select(pd => SignalProcessingService.ComputeFft(pd)).ToList();

        // Update frequency-domain plot
        var freqDomainSeries = Waveforms[0].FrequencyDomainModel.PlotModel.Series;
        for (var i = 0; i < fftData.Count && i < freqDomainSeries.Count; i++)
        {
            var series = (LineSeries)freqDomainSeries[i];
            var points = new List<DataPoint>(fftData[i].Length);
            var freqStep = SampleRate / processedData[i].Length;
            for (var j = 0; j < fftData[i].Length; j++) points.Add(new DataPoint(j * freqStep, fftData[i][j]));
            series.Points.Clear();
            series.Points.AddRange(points);
        }

        // 调整频域 X 轴范围与滤波带宽一致
        var xAxis = Waveforms[0].FrequencyDomainModel.PlotModel.Axes.First(a => a.Position == AxisPosition.Bottom);
        double nyquist = SampleRate / 2;
        double minFreq = 0;
        double maxFreq = nyquist;
        if (EnableFilter)
        {
            switch (FilterType)
            {
                case FilterType.LowPass:
                    maxFreq = LowPass;
                    break;
                case FilterType.HighPass:
                    minFreq = HighPass;
                    break;
                case FilterType.BandPass:
                    minFreq = BandPassFirst;
                    maxFreq = BandPassSecond;
                    break;
            }
            // 防止设置范围无效
            if (maxFreq <= 0) maxFreq = nyquist;
            if (minFreq < 0) minFreq = 0;
            if (maxFreq <= minFreq) maxFreq = minFreq + nyquist / 10;
        }
        xAxis.Minimum = minFreq;
        xAxis.Maximum = maxFreq;

        // 计算频域峰值（根据所有通道，使用" / "分隔）
        if (freqDomainSeries.Count > 0)
        {
            var freqPeakResults = freqDomainSeries.Cast<LineSeries>()
                .Select(ls => SignalProcessingService.CalculateFreqPeak(ls.Points.ToArray()))
                .ToList();
            string fmt(double d) => $"{d,8:0.000;0.000}"; // 始终带符号并固定 8 宽
            var channelStrings = freqPeakResults
                .Select(r => $"F:{fmt(r.Position)}Hz Pk:{fmt(r.Peak)} ({Waveforms[0].FrequencyDomainModel.PlotModel.YUnit})");
            Waveforms[0].FrequencyDomainModel.PlotModel.SubTitle = string.Join(" / ", channelStrings);
        }

        Waveforms[0].FrequencyDomainModel.PlotModel.InvalidatePlot(true);

        // 更新李萨如图 (I vs Q)
        if (SelectedSignalType == Parameter.DataChannelType.ISignalAndQSignal && processedData.Count >= 2)
        {
            var lissajousSeries = Waveforms[0].LissajousModel?.PlotModel.Series.FirstOrDefault() as ScatterSeries;
            if (lissajousSeries != null)
            {
                lissajousSeries.Points.Clear();
                var len = processedData[0].Length;//Math.Min(processedData[0].Length, 10240); // 限制点数
                for (int k = 0; k < len; k++)
                {
                    lissajousSeries.Points.Add(new ScatterPoint(processedData[0][k], processedData[1][k]));
                }
                Waveforms[0].LissajousModel.PlotModel.InvalidatePlot(true);

                // 计算相关系数与强度
                var iSig = processedData[0];
                var qSig = processedData[1];
                int lenSig = iSig.Length;//Math.Min(iSig.Length, 4096);
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
                double corr = cov / (stdI * stdQ);

                // 信号强度：向量幅值均方根
                double strengthRms = 0;
                for (int m = 0; m < lenSig; m++)
                    strengthRms += iSig[m] * iSig[m] + qSig[m] * qSig[m];
                strengthRms = Math.Sqrt(strengthRms / lenSig);

                string fmt8(double d) => $"{d,8:0.000}";
                Waveforms[0].LissajousModel.PlotModel.SubTitle = $"Corr:{fmt8(corr)}  RMS:{fmt8(strengthRms)}";

                Waveforms[0].Strength = Math.Min(1.0, strengthRms / 100.0);
            }
        }

        _timer.Start();
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
            TestStatus = $"停止测试失败:{e.Message}";
            StatusColor = new SolidColorBrush(Colors.Red);
        }
    }

    /// <summary>
    /// 停止测试后的统一清理逻辑，确保最多执行一次。
    /// </summary>
    private async Task CleanupAfterTestAsync(string finalStatus)
    {
        if (Interlocked.Exchange(ref _hasCleaned, 1) == 1) return;

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
                catch
                {
                    // 忽略停止设备时的异常
                }
            }

            SelectedSignalTypeEnable = true;
            IsTestRunning = false;
            ShoudUpdate = false;
            TriggerStatus = "准备就绪";

            TestStatus = finalStatus;
            StatusColor = new SolidColorBrush(Colors.Black);

            // 若进度未满，补齐
            SaveProgress = 100;
            ReceivedProgress = 100;
        }
        catch (Exception ex)
        {
            TestStatus = $"停止测试失败:{ex.Message}";
            StatusColor = new SolidColorBrush(Colors.Red);
        }
    }

    private void DeviceControlApi_DataReceived(Dictionary<Parameter.DataChannelType, double[,]> channelDatas)
    {
        if (ShoudUpdate && channelDatas.TryGetValue(SelectedSignalType, out var rawData))
        {
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

    private void SyncDeviceState()
    {
        if (_connectionVm.SelectedDevice != null)
        {
            _currentDevice = _connectionVm.SelectedDevice;
            ControlEnable = true;
            if (_currentDevice.DeviceParameters != null) UpdateParameters(_currentDevice.DeviceParameters);
        }
    }

    // 波形数据类
    public class Waveform : ReactiveObject
    {
        public Parameter.DataChannelType DataChannelType { get; set; }
        public CuPlotViewModel TimeDomainModel { get; set; }
        public CuPlotViewModel FrequencyDomainModel { get; set; }
        public CuPlotViewModel LissajousModel { get; set; }

        private double _strength;
        public double Strength
        {
            get => _strength;
            set => this.RaiseAndSetIfChanged(ref _strength, value);
        }

        public void InvalidatePlot(bool t)
        {
            TimeDomainModel.PlotModel.InvalidatePlot(t);
            FrequencyDomainModel.PlotModel.InvalidatePlot(t);
        }
    }

    #region Properties

    private int _displayPointCount = 10240;

    public int DisplayPointCount
    {
        get => _displayPointCount;
        set => this.RaiseAndSetIfChanged(ref _displayPointCount, value);
    }

    private Parameter.DataChannelType _selectedSignalType;

    public Parameter.DataChannelType SelectedSignalType
    {
        get => _selectedSignalType;
        set => this.RaiseAndSetIfChanged(ref _selectedSignalType, value);
    }

    private ObservableCollection<Waveform> _waveforms;

    public ObservableCollection<Waveform> Waveforms
    {
        get => _waveforms;
        set => this.RaiseAndSetIfChanged(ref _waveforms, value);
    }

    private ObservableCollection<Parameter.DataChannelType> _signalTypes;

    public ObservableCollection<Parameter.DataChannelType> SignalTypes
    {
        get => _signalTypes;
        set => this.RaiseAndSetIfChanged(ref _signalTypes, value);
    }

    private ObservableCollection<WindowFunction> _windowFunctions;

    public ObservableCollection<WindowFunction> WindowFunctions
    {
        get => _windowFunctions;
        set => this.RaiseAndSetIfChanged(ref _windowFunctions, value);
    }

    private WindowFunction _selectedWindowFunction;

    public WindowFunction SelectedWindowFunction
    {
        get => _selectedWindowFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedWindowFunction, value);
    }

    private bool _removeDc;

    public bool RemoveDc
    {
        get => _removeDc;
        set => this.RaiseAndSetIfChanged(ref _removeDc, value);
    }

    private bool _selectedSignalTypeEnable = true;

    public bool SelectedSignalTypeEnable
    {
        get => _selectedSignalTypeEnable;
        set => this.RaiseAndSetIfChanged(ref _selectedSignalTypeEnable, value);
    }

    private double _sampleRate;

    public double SampleRate
    {
        get => _sampleRate;
        set => this.RaiseAndSetIfChanged(ref _sampleRate, value);
    }

    private bool _controlEnable;

    public bool ControlEnable
    {
        get => _controlEnable;
        set => this.RaiseAndSetIfChanged(ref _controlEnable, value);
    }

    private List<EnumOption> _sampleRateList;

    public List<EnumOption> SampleRateList
    {
        get => _sampleRateList;
        set => this.RaiseAndSetIfChanged(ref _sampleRateList, value);
    }

    private EnumOption _selectedSampleRate;

    public EnumOption SelectedSampleRate
    {
        get => _selectedSampleRate;
        set => this.RaiseAndSetIfChanged(ref _selectedSampleRate, value);
    }

    private bool _enableFilter;

    public bool EnableFilter
    {
        get => _enableFilter;
        set => this.RaiseAndSetIfChanged(ref _enableFilter, value);
    }

    private FilterType _filterType = FilterType.LowPass;

    public FilterType FilterType
    {
        get => _filterType;
        set => this.RaiseAndSetIfChanged(ref _filterType, value);
    }

    private double _lowPass = 500;

    public double LowPass
    {
        get => _lowPass;
        set => this.RaiseAndSetIfChanged(ref _lowPass, value);
    }

    private double _highPass = 500;

    public double HighPass
    {
        get => _highPass;
        set => this.RaiseAndSetIfChanged(ref _highPass, value);
    }

    private double _bandPassFirst = 100;

    public double BandPassFirst
    {
        get => _bandPassFirst;
        set => this.RaiseAndSetIfChanged(ref _bandPassFirst, value);
    }

    private double _bandPassSecond = 500;

    public double BandPassSecond
    {
        get => _bandPassSecond;
        set => this.RaiseAndSetIfChanged(ref _bandPassSecond, value);
    }

    private double _saveProgress;

    public double SaveProgress
    {
        get => _saveProgress;
        set => this.RaiseAndSetIfChanged(ref _saveProgress, value);
    }

    private string _testStatus = "";

    public string TestStatus
    {
        get => _testStatus;
        set => this.RaiseAndSetIfChanged(ref _testStatus, value);
    }

    private bool _isTestRunning;

    public bool IsTestRunning
    {
        get => _isTestRunning;
        set => this.RaiseAndSetIfChanged(ref _isTestRunning, value);
    }

    private double _receivedProgress;

    public double ReceivedProgress
    {
        get => _receivedProgress;
        set => this.RaiseAndSetIfChanged(ref _receivedProgress, value);
    }

    private SolidColorBrush _statusColor = new(Colors.Black);

    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set => this.RaiseAndSetIfChanged(ref _statusColor, value);
    }

    private bool _showDataStorageInfo;

    public bool ShowDataStorageInfo
    {
        get => _showDataStorageInfo;
        set => this.RaiseAndSetIfChanged(ref _showDataStorageInfo, value);
    }

    private string _triggerStatus = "准备就绪";

    public string TriggerStatus
    {
        get => _triggerStatus;
        set => this.RaiseAndSetIfChanged(ref _triggerStatus, value);
    }

    private bool _shoudUpdate;

    public bool ShoudUpdate
    {
        get => _shoudUpdate;
        set => this.RaiseAndSetIfChanged(ref _shoudUpdate, value);
    }

    private TriggerType _selectedTriggerType;

    public TriggerType SelectedTriggerType
    {
        get => _selectedTriggerType;
        private set => this.RaiseAndSetIfChanged(ref _selectedTriggerType, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> StopTestCommand { get; }

    #endregion

}