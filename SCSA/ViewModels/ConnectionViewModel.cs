using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Services;
using SCSA.Services.Device;
using SCSA.ViewModels.Messages;
using SCSA.Utils;

namespace SCSA.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private readonly ParameterViewModel _parameterViewModel;
    private readonly IAppSettingsService _settingsService;
    private readonly PipelineTcpServer<PipelineNetDataPackage> _tcpServer;
    private readonly StatusBarViewModel _statusBar;

    private DispatcherTimer _statusDispatcherTimer;

    public ConnectionViewModel(PipelineTcpServer<PipelineNetDataPackage> tcpServer, IAppSettingsService settingsService,
        ParameterViewModel parameterViewModel, StatusBarViewModel statusBar)
    {
        _tcpServer = tcpServer;
        _settingsService = settingsService;
        _parameterViewModel = parameterViewModel;
        _statusBar = statusBar;

        try
        {
            var appSettings = _settingsService.Load();
            Port = appSettings.ListenPort;
            InitializeNetworkInterfaces();
            if (!string.IsNullOrEmpty(appSettings.SelectedInterfaceName))
                SelectedInterface = NetworkInterfaces.FirstOrDefault(ni => ni.Name == appSettings.SelectedInterfaceName);
        }
        catch (Exception e)
        {
            Log.Error("初始化连接视图模型失败", e);
            ShowNotification("加载设置失败，使用默认值");
        }

        // When SelectedDevice changes, send a message
        this.WhenAnyValue(x => x.SelectedDevice)
            .Skip(1) // Skip initial null value
            .Subscribe(device =>
            {
                if (device != null)
                {
                    _statusBar.ConnectedDevice = $"设备: {device.EndPoint}";
                    ShowNotification($"已选中设备: {device.EndPoint}");
                    _statusDispatcherTimer.Start();
                }
                else
                {
                    _statusBar.ConnectedDevice = "无连接";
                    _statusDispatcherTimer.Stop();
                    MessageBus.Current.SendMessage(new SupportedParametersChangedMessage(device));
                }

                // 无论连接或断开，都广播设备变化消息
                MessageBus.Current.SendMessage(new SelectedDeviceChangedMessage(device));
            });

        // Listen for parameter read/write requests
        MessageBus.Current.Listen<RequestReadParametersMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await ReadParametersAsync(SelectedDevice));

        MessageBus.Current.Listen<RequestWriteParametersMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async msg => await WriteParametersAsync(SelectedDevice, msg.Parameters));

        _tcpServer.ClientConnected += OnClientConnected;
        _tcpServer.ClientDisconnected += OnClientDisconnected;

        // Define commands
        var canStart = this.WhenAnyValue(x => x.SelectedInterface)
            .Select(ni => ni != null);
        StartCommand = ReactiveCommand.Create(StartServer, canStart);

        StopCommand = ReactiveCommand.Create(StopServer);

        DisconnectCommand = ReactiveCommand.Create<DeviceConnection>(DisconnectDevice);
        SelectDeviceCommand = ReactiveCommand.CreateFromTask<DeviceConnection>(SelectDeviceAsync);
        ReadParameterCommand = ReactiveCommand.CreateFromTask<DeviceConnection>(ReadParametersAsync);

        // Persist Port & Interface when changed
        this.WhenAnyValue(x => x.Port)
            .Skip(1)
            .Subscribe(p => SaveNetworkSettings());

        this.WhenAnyValue(x => x.SelectedInterface)
            .Skip(1)
            .Subscribe(_ => SaveNetworkSettings());

        ParameterViewModel = parameterViewModel;

        ToggleServerCommand = ReactiveCommand.Create(() =>
        {
            if (IsServerRunning)
                StopServer();
            else
                StartServer();
        }, canStart.Merge(this.WhenAnyValue(x=>x.IsServerRunning).Select(_=>true)));

        _statusDispatcherTimer = new DispatcherTimer();
        _statusDispatcherTimer.Interval = TimeSpan.FromSeconds(2);
        _statusDispatcherTimer.Tick += _statusDispatcherTimer_Tick;
    }

    private void _statusDispatcherTimer_Tick(object? sender, EventArgs e)
    {
        GetDeviceStatusAsync(SelectedDevice);
    }

    [Reactive] public int Port { get; set; } = 9123;

    [Reactive] public DeviceConnection SelectedDevice { get; set; }

    [Reactive] public NetworkInterfaceInfo SelectedInterface { get; set; }

    public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces { get; } = new();
    public ObservableCollection<DeviceConnection> ConnectedDevices { get; } = new();

    public ReactiveCommand<Unit, Unit> StartCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> DisconnectCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> SelectDeviceCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> ReadParameterCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleServerCommand { get; }

    public ParameterViewModel ParameterViewModel { get; }
    public bool ConnectionViewVisible => true;

    [Reactive]
    public bool IsServerRunning { get; private set; }

    private void StartServer()
    {
        try
        {
            var endPoint = GetEndPoint(SelectedInterface, Port);
            _tcpServer.Start(endPoint);
            _statusBar.ListeningEndpoint = $"正在监听: {SelectedInterface.Name}:{Port}";
            ShowNotification($"正在监听 {SelectedInterface.Name}:{Port}");
            IsServerRunning = true;
        }
        catch (Exception ex)
        {
            Log.Error("启动服务器失败", ex);
            ShowNotification($"启动失败: {ex.Message}");
        }
    }

    private void StopServer()
    {
        try
        {
            _tcpServer.Stop();
            _statusBar.ListeningEndpoint = "未监听";
            ShowNotification("已停止监听");
            IsServerRunning = false;
        }
        catch (Exception e)
        {
            Log.Error("停止服务器失败", e);
            ShowNotification($"停止失败: {e.Message}");
        }
    }

    private void DisconnectDevice(DeviceConnection device)
    {
        try
        {
            device?.Client?.Close();
            if (device != null)
            {
                ConnectedDevices.Remove(device);
                if (SelectedDevice == device)
                {
                    SelectedDevice = null;
                }
                ShowNotification($"已断开设备连接: {device.EndPoint}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"断开设备 {device?.EndPoint} 失败", e);
            ShowNotification($"断开设备失败: {e.Message}");
        }
    }

    private async Task SelectDeviceAsync(DeviceConnection device)
    {
        if (device != null)
        {
            SelectedDevice = device;
            await ReadParametersAsync(device);
        }
    }

    private void OnClientDisconnected(object sender, PipelineTcpClient<PipelineNetDataPackage> e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var device = ConnectedDevices.FirstOrDefault(d => Equals(d.EndPoint, e.RemoteEndPoint));
            if (device != null)
            {
                ConnectedDevices.Remove(device);
                if (SelectedDevice == device)
                {
                    SelectedDevice = null;
                }
                ShowNotification($"设备已断开连接: {e.RemoteEndPoint}");
            }
        });
    }

    private void OnClientConnected(object sender, PipelineTcpClient<PipelineNetDataPackage> e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var device = new DeviceConnection
            {
                EndPoint = e.RemoteEndPoint,
                ConnectTime = DateTime.Now,
                Client = e,
                DeviceParameters = new List<DeviceParameter>(),
                DeviceControlApi = new PipelineDeviceControlApiAsync(e)
            };

            ConnectedDevices.Add(device);
            ShowNotification($"新设备已连接: {e.RemoteEndPoint}");

            if (SelectedDevice == null) await SelectDeviceAsync(device);
        });
    }

    private async Task<bool> ReadParametersAsync(DeviceConnection device)
    {
        if(!await GetSupportParametersAsync(device))
            return false;

        if (device == null) return false;

        ShowNotification("正在读取参数...");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var parametersToRead = device.SupportParameterTypes
            .Select(p => new Parameter { Address = p }).ToList();

        try
        {
            var result = await device.DeviceControlApi.ReadParameters(parametersToRead, cts.Token);
            if (result.success)
            {
                var deviceParams = result.result.Select(p => (DeviceParameter)new NumberParameter
                    {
                        Address = (int)p.Address, Value = p.Value, DataLength = p.Length, Name = p.Address.ToString()
                    })
                    .ToList();
                device.DeviceParameters = deviceParams;
                ShowNotification("参数读取成功");


                MessageBus.Current.SendMessage(new ParametersChangedMessage(device));

                return true;
            }
            else
            {
                ShowNotification("参数读取失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"读取设备 {device?.DeviceId} 参数失败", ex);
            ShowNotification($"参数读取出错: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> GetSupportParametersAsync(DeviceConnection device)
    {
        if (device == null) return false;

        ShowNotification("正在读取参数列表...");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var result = await device.DeviceControlApi.GetParameterIds(cts.Token);
            if (result.success)
            {
                device.SupportParameterTypes = result.result.Select(r => r.Address).ToList();

                ShowNotification("参数读列表取成功");

                MessageBus.Current.SendMessage(new SupportedParametersChangedMessage(device));
                return true;
            }
            else
            {
                ShowNotification("参数列表读取失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"读取设备 {device?.DeviceId} 参数列表失败", ex);
            ShowNotification($"参数列表读取出错: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> GetDeviceStatusAsync(DeviceConnection device)
    {
        if (device == null) return false;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var readDeviceStatus = Enum.GetValues(typeof(DeviceStatusType)).Cast<DeviceStatusType>().ToList();
            var result = await device.DeviceControlApi.GetDeviceStatus(readDeviceStatus,cts.Token);
            if (result.success)
            {
                device.DeviceStatuses = result.result;
                MessageBus.Current.SendMessage(new DeviceStatusChangedMessage(device));
                return true;
            }

        }
        catch (Exception ex)
        {
            Log.Error($"读取设备 {device?.DeviceId} 参数列表失败", ex);

        }
        return false;
    }
    private async Task WriteParametersAsync(DeviceConnection device, List<Parameter> parameters)
    {
        if (device == null) return;
        ShowNotification("正在写入参数...");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var result = await device.DeviceControlApi.SetParameters(parameters, cts.Token);
            if (result)
            {
                //await ReadParametersAsync(device);
                ShowNotification("参数写入成功");
            }
            else
            {
                ShowNotification("参数写入失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"写入设备 {device?.DeviceId} 参数失败", ex);
            ShowNotification($"参数写入出错: {ex.Message}");
        }
    }

    private void InitializeNetworkInterfaces()
    {
        try
        {
            var physicalInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni =>
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Select(ni => new NetworkInterfaceInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    IPAddress = GetIpAddress(ni),
                    RawInterface = ni
                })
                .OrderBy(ni => ni.Name);

            foreach (var ni in physicalInterfaces) NetworkInterfaces.Add(ni);
            SelectedInterface = NetworkInterfaces.FirstOrDefault();
        }
        catch (Exception e)
        {
            Log.Error("初始化网络接口列表失败", e);
            ShowNotification("无法获取网络接口列表");
        }
    }

    private string GetIpAddress(NetworkInterface networkInterface)
    {
        return networkInterface.GetIPProperties().UnicastAddresses
                   .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?.Address
                   ?.ToString() ??
               "无 IP 地址";
    }

    private IPEndPoint GetEndPoint(NetworkInterfaceInfo networkInterface, int port)
    {
        if (networkInterface == null) throw new ArgumentNullException(nameof(networkInterface));
        var ipAddress = networkInterface.RawInterface.GetIPProperties().UnicastAddresses
            .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
        if (ipAddress == null) throw new InvalidOperationException("未找到有效的 IPv4 地址");
        return new IPEndPoint(ipAddress, port);
    }

    private void SaveNetworkSettings()
    {
        try
        {
            var s = _settingsService.Load();
            s.ListenPort = Port;
            s.SelectedInterfaceName = SelectedInterface?.Name ?? string.Empty;
            _settingsService.Save(s);
        }
        catch (Exception e)
        {
            Log.Error("保存网络设置失败", e);
            ShowNotification("保存网络设置失败");
        }
    }
}