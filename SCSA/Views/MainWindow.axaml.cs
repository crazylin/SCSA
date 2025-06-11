using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using SCSA.ViewModels;
using FluentAvalonia.UI.Windowing;
using FluentAvalonia.Styling;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.UI.Media;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Controls.Chrome;
using Avalonia.VisualTree;

using Avalonia.Controls.ApplicationLifetimes;
using System.Diagnostics;
using System.Linq;
using OxyPlot;
using System.Drawing;
using OxyPlot.Avalonia;
using Color = Avalonia.Media.Color;
using SCSA.Utils;

namespace SCSA.Views
{
    public partial class MainWindow : AppWindow
    {
        private readonly Dictionary<string, object> _viewCache = new();
        public MainWindow()
        {
            
            InitializeComponent();
            //SplashScreen = new MainAppSplashScreen(this);
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 25;
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            Application.Current.ActualThemeVariantChanged += Current_ActualThemeVariantChanged;

            this.DataContextChanged += MainWindow_DataContextChanged;
  
        }

        private void Current_ActualThemeVariantChanged(object? sender, System.EventArgs e)
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
                    ThemeVariant.Dark, out var value) ? (Color2)(Color)value : new Color2(32, 32, 32);

                color = color.LightenPercent(-0.8f);

                Background = new ImmutableSolidColorBrush(color, 0.9);
            }
            else if (ActualThemeVariant == ThemeVariant.Light)
            {
                // Similar effect here
                var color = this.TryFindResource("SolidBackgroundFillColorBase",
                    ThemeVariant.Light, out var value) ? (Color2)(Color)value : new Color2(243, 243, 243);

                color = color.LightenPercent(0.5f);

                Background = new ImmutableSolidColorBrush(color, 0.9);
            }
        }
        private void MainWindow_DataContextChanged(object? sender, System.EventArgs e)
        {
            // 绑定 MVVM
            if (DataContext is MainWindowViewModel vm)
            {
                // 监听 SelectedItem 改变
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(vm.SelectedItem))
                        UpdateContent(vm);
                };
                // 初始化第一次
                UpdateContent(vm);
            }
        }

        private void UpdateContent(MainWindowViewModel vm)
        {
            if (vm.SelectedItem == null)
            {
                ContentControl.Content = null;
                return;
            }
            dynamic selectItem = vm.SelectedItem;
            string tag = selectItem.Tag.ToString();

            if (!_viewCache.TryGetValue(tag, out var view))
            {
                // 第一次创建，注入 DataContext
                view = tag switch
                {
                    "0" => new ConnectionView { DataContext = vm.ConnectionViewModel },
                    "1" => new RealTimeTestView { DataContext = vm.RealTimeTestViewModel },
                    "2" => new FirmwareUpdateView { DataContext = vm.FirmwareUpdateViewModel },
                    "3" => new SettingsView() { DataContext = vm.SettingsViewModel },
                    _ => new UserControl()
                };
                _viewCache[tag] = view;
            }

            // 切换 ContentControl
            ContentControl.Content = view;
        }


        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is NavigationView { MenuItems: { Count: > 0 } } navView)
            {
                var vm = (MainWindowViewModel)DataContext;
                vm.SelectedItem = navView.MenuItems[0];
            }
        }
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
}