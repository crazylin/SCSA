using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using SCSA.Plot;
using SCSA.Utils;

namespace SCSA.Plot;

public class CuPlotViewModel : ReactiveObject
{
    public CuPlotViewModel()
    {
        // Property Change Subscriptions
        this.WhenAnyValue(x => x.SelectedMode)
            .Skip(1)
            .Subscribe(sm => _plotModel.SelectedMode = sm);

        this.WhenAnyValue(x => x.IsLogEnabled)
            .Skip(1)
            .Subscribe(sm => _plotModel.ToggleLog(_isLogEnabled));

        this.WhenAnyValue(x => x.IsLockEnabled)
            .Skip(1)
            .Subscribe(sm => _plotModel.ToggleLog(_isLockEnabled));

        this.WhenAnyValue(x => x.PlotModel)
            .Skip(1)
            .Subscribe(_ =>
            {
                if(_plotModel==null)
                    return;
                _plotModel.SelectedMode = _selectedMode;
                _plotModel.ToggleLog(_isLogEnabled);
                _plotModel.ToggleLock(_isLockEnabled);
            });



        CopyCommand = ReactiveCommand.Create(() => PlotModel.CopyAlignedSeriesDataToClipboard());
        ResetCommand = ReactiveCommand.Create(DoReset);
        ScreenshotInteraction = new Interaction<Unit, Unit>();
        ScreenshotCommand = ReactiveCommand.CreateFromTask(async () => await ScreenshotInteraction.Handle(Unit.Default));
    }

    private void DoReset()
    {
        _plotModel?.ResetPlot();
        IsLogEnabled = false;
        IsLockEnabled = false;
        SelectedMode = InteractionMode.None;
    }

    public CuPlotModel PlotModel
    {
        set=> this.RaiseAndSetIfChanged(ref _plotModel, value);
        get => _plotModel;
    }

    private InteractionMode _selectedMode;
    public InteractionMode SelectedMode
    {
        get => _selectedMode;
        set => this.RaiseAndSetIfChanged(ref _selectedMode, value);
    }

    private bool _isLogEnabled;
    public bool IsLogEnabled
    {
        get => _isLogEnabled;
        set => this.RaiseAndSetIfChanged(ref _isLogEnabled, value);
    }

    private bool _isLockEnabled;
    private CuPlotModel _plotModel;

    public bool IsLockEnabled
    {
        get => _isLockEnabled;
        set=> this.RaiseAndSetIfChanged(ref _isLockEnabled, value);
    }

    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> ScreenshotCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    // View 负责实现截图逻辑
    public Interaction<Unit, Unit> ScreenshotInteraction { get; }
} 