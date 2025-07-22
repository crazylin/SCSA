using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Services;
using SCSA.Services.Recording;
using SCSA.Services.Recording.Writers;
using SCSA.ViewModels;
using SCSA.Views;
using System;
using System.IO;
using System.Linq;
using SCSA.Utils;

namespace SCSA;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            services.AddSingleton(desktop.MainWindow.StorageProvider);

            var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });

            // 初始化日志系统
            var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            var appSettings = appSettingsService.Load();
            Log.Initialize(appSettings.EnableLogging);

            desktop.MainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();

            // 应用退出时关闭日志后台线程
            desktop.Exit += (_, _) => Log.Shutdown();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            //singleView.MainView = new MainView
            //{
            //    DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            //};
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ConnectionViewModel>();
        services.AddSingleton<ParameterViewModel>();
        services.AddSingleton<RealTimeTestViewModel>();
        services.AddSingleton<DebugParameterViewModel>();
        services.AddSingleton<FirmwareUpdateViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<PlaybackViewModel>();
        services.AddSingleton<PulseOutputViewModel>(provider => 
            new PulseOutputViewModel(
                provider.GetRequiredService<ConnectionViewModel>(),
                provider.GetRequiredService<IAppSettingsService>()));

        // Services
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<PipelineTcpServer<PipelineNetDataPackage>>();
        services.AddSingleton<IRecorderService, RecorderService>();

        // Views
        services.AddTransient<ConnectionView>();
        services.AddTransient<RealTimeTestView>();
        services.AddTransient<DebugParameterView>();
        services.AddTransient<FirmwareUpdateView>();
        services.AddTransient<ParameterView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<PlaybackView>();
        services.AddTransient<PulseOutputView>();
    }
}