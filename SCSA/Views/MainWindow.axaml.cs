using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;

using SCSA.ViewModels;
using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Threading;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;


namespace SCSA.Views;

public partial class MainWindow : AppWindow
{
    private TeachingTip _notificationTip;
    private CancellationTokenSource _notifCts;
    public MainWindow()
    {
        InitializeComponent();
        //SplashScreen = new MainAppSplashScreen(this);
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 32;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        ViewModelBase.NotificationRequested += OnNotificationRequested;

        //Application.Current.ActualThemeVariantChanged += Current_ActualThemeVariantChanged;
    }

    private async void OnNotificationRequested(string message, FluentAvalonia.UI.Controls.InfoBarSeverity severity)
    {
        Debug.WriteLine(message);
        NotificationInfoBar.Title = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;

        // 重置隐藏计时器，只以最后一次通知为准
        _notifCts?.Cancel();
        _notifCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(3000, _notifCts.Token);
            NotificationInfoBar.IsOpen = false;
        }
        catch (TaskCanceledException)
        {
            // 新通知到来，计时器被取消，忽略
        }

        //if (_notificationTip == null)
        //{
        //    _notificationTip = new TeachingTip
        //    {
        //        Title = "提示",
        //        Subtitle = message,
        //        IsLightDismissEnabled = true,
        //        PreferredPlacement = TeachingTipPlacementMode.Auto,
        //        IsOpen = true,
        //    };

        //    // 获取主窗口并添加通知
        //    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        //    {
        //        if (desktop.MainWindow?.Content is Panel panel)
        //        {
        //            panel.Children.Add(_notificationTip);
        //        }
        //    }
        //}
        //else
        //{
        //    _notificationTip.Subtitle = message;

        //    _notificationTip.IsOpen = true;
        //}

        //// 3秒后自动关闭
        //Task.Delay(3000).ContinueWith(_ =>
        //{
        //    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        //    {
        //        _notificationTip.IsOpen = false;
        //    });
        //});
    }

    private void Current_ActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (IsWindows11)
        {
            if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
            {
                TryEnableMicaEffect();
            }
            else
            {
                ClearValue(BackgroundProperty);
                ClearValue(TransparencyBackgroundFallbackProperty);
            }
        }
    }

    private void TryEnableMicaEffect()
    {
        //return;
        // TransparencyBackgroundFallback = Brushes.Transparent;
        // TransparencyLevelHint = WindowTransparencyLevel.Mica;

        // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
        // BUT since we can't control the actual Mica brush color, we have to use the window background to create
        // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
        // we set the opacity the color become lighter than we want. So we take the normal color, darken it and 
        // apply the opacity until we get the roughly the correct color
        // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
        // CompositionBrush to properly change the color but I don't know if we can do that or not
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Dark, out var value)
                ? (Color2)(Color)value
                : new Color2(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
        else if (ActualThemeVariant == ThemeVariant.Light)
        {
            // Similar effect here
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                ThemeVariant.Light, out var value)
                ? (Color2)(Color)value
                : new Color2(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }

    private void ThemeToggle_Click(object? sender, RoutedEventArgs e)
    {
        var newTheme = Application.Current.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        Application.Current.RequestedThemeVariant = newTheme;

        // 可根据需要更新按钮外观
    }

    //private void NavView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    //{
    //    if (DataContext is MainWindowViewModel vm)
    //    {
    //        if (e.SelectedItem is NavigationViewItem nvi && nvi.DataContext is MainWindowViewModel.NavItem nav)
    //        {
    //            vm.SelectedItem = nav;
    //        }
    //    }
    //}
}

//internal class MainAppSplashScreen : IApplicationSplashScreen
//{
//    public MainAppSplashScreen(MainWindow owner)
//    {
//        _owner = owner;
//    }

//    public string AppName { get; }
//    public IImage AppIcon { get; }
//    public object SplashScreenContent => new MainAppSplashContent();
//    public int MinimumShowTime => 2000;

//    public Action InitApp { get; set; }

//    public Task RunTasks(CancellationToken cancellationToken)
//    {
//        if (InitApp == null)
//            return Task.CompletedTask;

//        return Task.Run(InitApp, cancellationToken);
//    }

//    private MainWindow _owner;
//}