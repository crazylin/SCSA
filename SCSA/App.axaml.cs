using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Utils;
using SCSA.ViewModels;
using SCSA.Views;
using Serilog;

namespace SCSA
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Async(a => a.File("logs/SCSA.log", rollingInterval: RollingInterval.Day))
                .CreateLogger();

            // 设置主窗口或主视图的 DataContext
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                desktop.MainWindow = new MainWindow();

                // 手动注册 IStorageProvider（从 MainWindow 获取）
                services.AddSingleton(desktop.MainWindow.StorageProvider);

                // 重新构建 ServiceProvider
                var serviceProvider = services.BuildServiceProvider();

                // 设置 DataContext
                desktop.MainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();

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
            // 注册 ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ConnectionViewModel>();
            services.AddSingleton<ParameterViewModel>();
            services.AddSingleton<RealTimeTestViewModel>();
            services.AddSingleton<FirmwareUpdateViewModel>();

            // 注册 Services
            // services.AddSingleton<ITcpServer, TcpServer>();

            //services.AddSingleton<ITcpServer<NetDataPackage>, EasyTcpServer<NetDataPackage>>();
            services.AddSingleton<PipelineTcpServer<PipelineNetDataPackage>>();
            // 注册 Views
            services.AddTransient<MainWindow>();
            services.AddTransient<ConnectionView>();
            services.AddTransient<RealTimeTestView>();
            services.AddTransient<FirmwareUpdateView>();
            services.AddTransient<ParameterView>();


        }
    }
}