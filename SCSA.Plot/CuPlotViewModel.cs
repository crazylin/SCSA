using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using SCSA.Plot;
using SCSA.Utils;
using ReactiveUI.Fody.Helpers;

namespace SCSA.Plot;

public class CuPlotViewModel : ReactiveObject
{
    public CuPlotViewModel()
    {
        // Property Change Subscriptions
        this.WhenAnyValue(x => x.SelectedMode)
            .Skip(1)
            .Subscribe(sm => { if (PlotModel != null) PlotModel.SelectedMode = sm; });

        this.WhenAnyValue(x => x.IsLogEnabled)
            .Skip(1)
            .Subscribe(flag => { if (PlotModel != null) PlotModel.ToggleLog(flag); });

        this.WhenAnyValue(x => x.IsLockEnabled)
            .Skip(1)
            .Subscribe(flag => { if (PlotModel != null) PlotModel.ToggleLock(flag); });

        this.WhenAnyValue(x => x.PlotModel)
            .Skip(1)
            .Subscribe(_ =>
            {
                if(PlotModel==null)
                    return;
                PlotModel.SelectedMode = SelectedMode;
                PlotModel.ToggleLog(IsLogEnabled);
                PlotModel.ToggleLock(IsLockEnabled);
            });



        CopyCommand = ReactiveCommand.Create(() => PlotModel.CopyAlignedSeriesDataToClipboard());
        ResetCommand = ReactiveCommand.Create(DoReset);
        ScreenshotInteraction = new Interaction<Unit, Unit>();
        ScreenshotCommand = ReactiveCommand.CreateFromTask(async () => await ScreenshotInteraction.Handle(Unit.Default));
    }

    private void DoReset()
    {
        PlotModel?.ResetPlot();
        IsLogEnabled = false;
        IsLockEnabled = false;
        SelectedMode = InteractionMode.None;
    }

    [Reactive] public InteractionMode SelectedMode { get; set; }

    [Reactive] public bool IsLogEnabled { get; set; }

    [Reactive] public bool IsLockEnabled { get; set; }

    [Reactive] public CuPlotModel PlotModel { get; set; }

    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> ScreenshotCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    // View 负责实现截图逻辑
    public Interaction<Unit, Unit> ScreenshotInteraction { get; }
} 