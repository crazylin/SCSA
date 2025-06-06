using System;
using System.Collections.Generic;
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
using Serilog.Core;
using Serilog.Enrichers.CallerInfo;
using Serilog.Events;

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
                .Enrich.WithCallerInfo(
                    includeFileInfo: true,
                    assemblyPrefix: "SCSA",
                    prefix: "",             // ����ǰ׺
                    filePathDepth: 1,       // ֻ��ʾ�ļ���
                    excludedPrefixes: new List<string>
                        {
                            "System",
                            "Microsoft",
                            "Serilog",
                        })
                .Enrich.With(new CallerEnricher())
                .WriteTo.Async(a => a.File(
                    "logs/SCSA.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] <{Module}>::{Method} {Message:lj}{NewLine}{Exception}"
                )).CreateLogger();

            // ���������ڻ�����ͼ�� DataContext
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                desktop.MainWindow = new MainWindow();

                // �ֶ�ע�� IStorageProvider���� MainWindow ��ȡ��
                services.AddSingleton(desktop.MainWindow.StorageProvider);

                // ���¹��� ServiceProvider
                var serviceProvider = services.BuildServiceProvider();

                // ���� DataContext
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
            // ע�� ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ConnectionViewModel>();
            services.AddSingleton<ParameterViewModel>();
            services.AddSingleton<RealTimeTestViewModel>();
            services.AddSingleton<FirmwareUpdateViewModel>();

            // ע�� Services
            // services.AddSingleton<ITcpServer, TcpServer>();

            //services.AddSingleton<ITcpServer<NetDataPackage>, EasyTcpServer<NetDataPackage>>();
            services.AddSingleton<PipelineTcpServer<PipelineNetDataPackage>>();
            // ע�� Views
            services.AddTransient<MainWindow>();
            services.AddTransient<ConnectionView>();
            services.AddTransient<RealTimeTestView>();
            services.AddTransient<FirmwareUpdateView>();
            services.AddTransient<ParameterView>();


        }


        // �Զ���ḻ�����񷽷���������
        public class CallerEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                // ������ܱ���Ķ�ջ֡
                var skipFrames = 3;
                while (true)
                {
                    var stackFrame = new StackFrame(skipFrames);
                    if (!stackFrame.HasMethod())
                    {
                        logEvent.AddPropertyIfAbsent(
                            new LogEventProperty("Module", new ScalarValue("<unknown>")));
                        logEvent.AddPropertyIfAbsent(
                            new LogEventProperty("Method", new ScalarValue("<unknown>")));
                        return;
                    }

                    var method = stackFrame.GetMethod();
                    if (method.DeclaringType.Assembly != typeof(Log).Assembly)
                    {
                        var moduleName = method.DeclaringType?.FullName ?? "<unknown>";
                        var methodName = method.Name;

                        logEvent.AddPropertyIfAbsent(
                            propertyFactory.CreateProperty("Module", moduleName));
                        logEvent.AddPropertyIfAbsent(
                            propertyFactory.CreateProperty("Method", methodName));
                        return;
                    }

                    skipFrames++;
                }
            }
        }

    }
}