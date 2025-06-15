using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using Color = Avalonia.Media.Color;

namespace SCSA.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        //SplashScreen = new MainAppSplashScreen(this);
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 32;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        //Application.Current.ActualThemeVariantChanged += Current_ActualThemeVariantChanged;
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