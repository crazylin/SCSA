using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using OxyPlot.Series;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Plot;
using SCSA.Services;
using SCSA.UFF;
using SCSA.Utils;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace SCSA.ViewModels
{
    public class FileTreeNode : ReactiveObject
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public string IconSymbol { get; set; }
        public ObservableCollection<FileTreeNode> Children { get; set; }

        public FileTreeNode()
        {
            Children = new ObservableCollection<FileTreeNode>();
        }
    }

    public class PlaybackViewModel : ViewModelBase
    {
        private readonly IAppSettingsService _appSettingsService;
        private IDisposable _playbackSubscription;
        [Reactive] private double[] LoadedSamples { get; set; } // Store the full data of the first channel
        private double _loadedSampleRate;

        [Reactive] public int DisplayPointCount { get; set; } = 4096;
        [Reactive] public double SelectedPlaybackSpeed { get; set; } = 1.0;
        public ObservableCollection<double> PlaybackSpeeds { get; } = new() { 0.25, 0.5, 1.0, 2.0, 4.0 };
        
        [Reactive] public ObservableCollection<FileTreeNode> FileTree { get; private set; }
        [Reactive] public FileTreeNode SelectedFileNode { get; set; }

        [Reactive] public double Duration { get; private set; }
        [Reactive] public double CurrentPosition { get; set; }
        [Reactive] public bool IsPlaying { get; private set; }

        public CuPlotViewModel OverviewPlotViewModel { get; }
        public CuPlotViewModel TimeDomainPlotViewModel { get; }
        public CuPlotViewModel FrequencyDomainPlotViewModel { get; }

        public ReactiveCommand<Unit, Unit> PlayCommand { get; }
        public ReactiveCommand<Unit, Unit> PauseCommand { get; }
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        public PlaybackViewModel(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService;
            
            // --- Plot ViewModels ---
            OverviewPlotViewModel = new CuPlotViewModel { PlotModel = new CuPlotModel { Title = "全览" } };
            TimeDomainPlotViewModel = new CuPlotViewModel { PlotModel = new CuPlotModel { Title = "时域" } };
            FrequencyDomainPlotViewModel = new CuPlotViewModel { PlotModel = new CuPlotModel { Title = "频域" } };

            OverviewPlotViewModel.PlotModel.SelectedMode = InteractionMode.RangeSelect;

            // --- Commands ---
            var canPlay = this.WhenAnyValue(x => x.LoadedSamples, x => x.IsPlaying, (data, playing) => data != null && data.Length > 0 && !playing);
            PlayCommand = ReactiveCommand.Create(Play, canPlay);
            PlayCommand.ThrownExceptions.Subscribe(ex => Log.Error("Play command failed", ex));

            PauseCommand = ReactiveCommand.Create(Pause, this.WhenAnyValue(x => x.IsPlaying));
            PauseCommand.ThrownExceptions.Subscribe(ex => Log.Error("Pause command failed", ex));

            StopCommand = ReactiveCommand.Create(Stop, this.WhenAnyValue(x => x.IsPlaying));
            StopCommand.ThrownExceptions.Subscribe(ex => Log.Error("Stop command failed", ex));

            // --- File Tree ---
            FileTree = new ObservableCollection<FileTreeNode>();
            LoadFileTree();

            // --- Subscriptions ---
            this.WhenAnyValue(x => x.SelectedFileNode)
                .Where(node => node != null && !node.IsDirectory)
                .Subscribe(node => LoadFile(node.FullPath));

            // When range is selected in overview, update the detailed plots
            this.WhenAnyValue(x => x.OverviewPlotViewModel.PlotModel.RangeResults)
                .Where(results => results != null && results.Count > 0)
                .Subscribe(results =>
                {
                    var range = results.First();
                    UpdateDetailedPlots(range.LeftPoint.X);
                });

            this.WhenAnyValue(x => x.CurrentPosition)
                .Subscribe(pos =>
                {
                    // Also move the selection rectangle on the overview plot
                    OverviewPlotViewModel.PlotModel.UpdatePlaybackCursor(pos);
                    UpdateDetailedPlots(pos);
                });
        }

        private void UpdateDetailedPlots(double startTime)
        {
            try
            {
                if (LoadedSamples == null) return;

                int startIdx = (int)(startTime * _loadedSampleRate);
                int pointsToTake = Math.Min(DisplayPointCount, LoadedSamples.Length - startIdx);
                if (pointsToTake <= 0) return;

                var segment = new ArraySegment<double>(LoadedSamples, startIdx, pointsToTake).ToArray();

                // Update Time Domain Plot
                TimeDomainPlotViewModel.PlotModel.ResetPlot();
                var timeSeries = new LineSeries { Title = "Time" };
                var timePoints = segment.Select((s, i) => new OxyPlot.DataPoint(startTime + i / _loadedSampleRate, s));
                timeSeries.Points.AddRange(timePoints);
                TimeDomainPlotViewModel.PlotModel.Series.Add(timeSeries);
                TimeDomainPlotViewModel.PlotModel.InvalidatePlot(true);
                
                // Update Frequency Domain Plot
                FrequencyDomainPlotViewModel.PlotModel.ResetPlot();
                var freqSeries = new LineSeries { Title = "Frequency" };
                // FFT
                var complexData = segment.Select(s => new Complex(s, 0)).ToArray();
                Fourier.Forward(complexData, FourierOptions.NoScaling);
                var freqPoints = complexData.Take(complexData.Length / 2)
                    .Select((c, i) => new OxyPlot.DataPoint(i * _loadedSampleRate / complexData.Length, c.Magnitude));
                freqSeries.Points.AddRange(freqPoints);
                FrequencyDomainPlotViewModel.PlotModel.Series.Add(freqSeries);
                FrequencyDomainPlotViewModel.PlotModel.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to update detailed plots", ex);
            }
        }

        private void Play()
        {
            IsPlaying = true;
            _playbackSubscription = Observable.Interval(TimeSpan.FromMilliseconds(100 / SelectedPlaybackSpeed), RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    // Move by a fraction of a second, not a fixed point count
                    CurrentPosition += 0.1; 
                    if (CurrentPosition >= Duration) Stop();
                });
        }
        private void Pause()
        {
            IsPlaying = false;
            _playbackSubscription?.Dispose();
        }
        private void Stop()
        {
            try
            {
                IsPlaying = false;
                _playbackSubscription?.Dispose();
                CurrentPosition = 0;
            }
            catch(Exception ex)
            {
                Log.Error("Failed to stop playback", ex);
            }
        }

        private void LoadFileTree()
        {
            var rootPath = _appSettingsService.Load().DataStoragePath;
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
            }

            var rootNode = new FileTreeNode { Name = Path.GetFileName(rootPath), FullPath = rootPath, IsDirectory = true };
            FileTree.Add(rootNode);
            PopulateChildren(rootNode);
        }

        private void PopulateChildren(FileTreeNode parentNode)
        {
            try
            {
                var directories = Directory.GetDirectories(parentNode.FullPath);
                foreach (var dir in directories)
                {
                    var childNode = new FileTreeNode { Name = Path.GetFileName(dir), FullPath = dir, IsDirectory = true, IconSymbol = "FolderOpen" };
                    parentNode.Children.Add(childNode);
                    PopulateChildren(childNode); // Recurse
                }

                var files = Directory.GetFiles(parentNode.FullPath)
                                     .Where(f => f.EndsWith(".uff", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
                foreach (var file in files)
                {
                    var fileExtension = Path.GetExtension(file).ToLowerInvariant();
                    string icon = fileExtension switch
                    {
                        ".wav" => "MusicInfo",
                        ".uff" => "Page",
                        _ => "Document"
                    };
                    parentNode.Children.Add(new FileTreeNode { Name = Path.GetFileName(file), FullPath = file, IsDirectory = false, IconSymbol = icon });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error populating file tree for {parentNode.FullPath}", ex);
            }
        }
        
        private void LoadFile(string filePath)
        {
            try
            {
                // Clear previous data
                LoadedSamples = null;
                OverviewPlotViewModel.PlotModel.ResetPlot();

                var extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".uff")
                {
                    var timeSeries = UFFReader.Read(filePath);
                    if (timeSeries.Any())
                    {
                        var firstChannel = timeSeries.First();
                        LoadedSamples = firstChannel.Samples;
                        _loadedSampleRate = firstChannel.SampleRate;
                    }
                }
                else if (extension == ".wav")
                {
                    var wavData = WavReader.Read(filePath);
                    if (wavData.Channels > 0)
                    {
                        LoadedSamples = wavData.Samples[0]; // Only first channel for now
                        _loadedSampleRate = wavData.SampleRate;
                    }
                }
                
                if (LoadedSamples != null)
                {
                    Duration = LoadedSamples.Length / _loadedSampleRate;
                    var series = new LineSeries { Title = "Overview" };
                    var points = LoadedSamples.Select((s, i) => new OxyPlot.DataPoint(i / _loadedSampleRate, s));
                    series.Points.AddRange(points);
                    OverviewPlotViewModel.PlotModel.Series.Add(series);
                    OverviewPlotViewModel.PlotModel.InvalidatePlot(true);

                    // Load initial detail view
                    UpdateDetailedPlots(0);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Failed to load playback file {filePath}", ex);
                ShowNotification($"加载文件失败: {ex.Message}", InfoBarSeverity.Error);
            }
        }
    }
} 