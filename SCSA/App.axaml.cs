using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Services;
using SCSA.Services.Recording;
using SCSA.ViewModels;
using SCSA.Views;

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
            SCSA.Utils.Log.Initialize(appSettings.EnableLogging);

            desktop.MainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();

            // 应用退出时关闭日志后台线程
            desktop.Exit += (_, _) => SCSA.Utils.Log.Shutdown();
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
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ConnectionViewModel>();
        services.AddSingleton<ParameterViewModel>();
        services.AddSingleton<RealTimeTestViewModel>();
        services.AddSingleton<FirmwareUpdateViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<PipelineTcpServer<PipelineNetDataPackage>>();
        services.AddSingleton<IRecorderService, RecorderService>();

        services.AddTransient<MainWindow>();
        services.AddTransient<ConnectionView>();
        services.AddTransient<RealTimeTestView>();
        services.AddTransient<FirmwareUpdateView>();
        services.AddTransient<ParameterView>();
        services.AddTransient<SettingsView>();
    }
}