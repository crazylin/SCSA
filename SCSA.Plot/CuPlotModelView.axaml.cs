using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using SCSA.Utils;
using Avalonia.ReactiveUI;
using System.Reactive;
using Avalonia.Markup.Xaml;
using System.Reactive.Disposables;
using SCSA.Plot;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OxyPlot.Avalonia;

namespace SCSA.Plot;

public partial class CuPlotModelView : Avalonia.ReactiveUI.ReactiveUserControl<CuPlotViewModel>
{
    public CuPlotModelView()
    {
        InitializeComponent();

        // 注册 Interaction 处理程序，防止 "Failed to find a registration for a Interaction" 异常
        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
                return;

            ViewModel.ScreenshotInteraction.RegisterHandler(async interaction =>
            {
                var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
                await ScreenshotHelper.CaptureAndSaveControlAsync(this, manWin);
                interaction.SetOutput(Unit.Default);
                await Task.CompletedTask;
            }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}