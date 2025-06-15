using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.Services;
using SCSA.Services.Device;
using SCSA.ViewModels.Messages;

namespace SCSA.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private readonly ParameterViewModel _parameterViewModel;
    private readonly IAppSettingsService _settingsService;
    private readonly PipelineTcpServer<PipelineNetDataPackage> _tcpServer;

    private int _port = 9123;

    private DeviceConnection _selectedDevice;

    private NetworkInterfaceInfo _selectedInterface;

    private string _statusMessage = "准备就绪";

    public ConnectionViewModel(PipelineTcpServer<PipelineNetDataPackage> tcpServer, IAppSettingsService settingsService,
        ParameterViewModel parameterViewModel)
    {
        _tcpServer = tcpServer;
        _settingsService = settingsService;
        _parameterViewModel = parameterViewModel;

        var appSettings = _settingsService.Load();
        Port = appSettings.ListenPort;

        InitializeNetworkInterfaces();

        // restore interface
        if (!string.IsNullOrEmpty(appSettings.SelectedInterfaceName))
            SelectedInterface = NetworkInterfaces.FirstOrDefault(ni => ni.Name == appSettings.SelectedInterfaceName);

        // When SelectedDevice changes, send a message
        this.WhenAnyValue(x => x.SelectedDevice)
            .Skip(1) // Skip initial null value
            .Subscribe(device =>
            {
                if (device != null)
                {
                    ShowNotification($"已选中设备: {device.DeviceId}");
                    MessageBus.Current.SendMessage(new SelectedDeviceChangedMessage(device));
                }
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
    }

    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    public DeviceConnection SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public NetworkInterfaceInfo SelectedInterface
    {
        get => _selectedInterface;
        set => this.RaiseAndSetIfChanged(ref _selectedInterface, value);
    }

    public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces { get; } = new();
    public ObservableCollection<DeviceConnection> ConnectedDevices { get; } = new();

    public ReactiveCommand<Unit, Unit> StartCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> DisconnectCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> SelectDeviceCommand { get; }
    public ReactiveCommand<DeviceConnection, Unit> ReadParameterCommand { get; }

    public ParameterViewModel ParameterViewModel { get; }
    public bool ConnectionViewVisible => true;

    private void StartServer()
    {
        try
        {
            var endPoint = GetEndPoint(SelectedInterface, Port);
            _tcpServer.Start(endPoint);
            ShowNotification($"正在监听 {SelectedInterface.Name}:{Port}");
        }
        catch (Exception ex)
        {
            ShowNotification($"启动失败: {ex.Message}");
        }
    }

    private void StopServer()
    {
        _tcpServer.Stop();
        ShowNotification("已停止监听");
    }

    private void DisconnectDevice(DeviceConnection device)
    {
        device.Client.Close();
        ConnectedDevices.Remove(device);
        ShowNotification($"已断开设备连接: {device.EndPoint}");
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

    private async Task ReadParametersAsync(DeviceConnection device)
    {
        if (device == null) return;

        ShowNotification("正在读取参数...");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var parametersToRead = Enum.GetValues<ParameterType>()
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
                MessageBus.Current.SendMessage(new ParametersChangedMessage(device));
                ShowNotification("参数读取成功");
            }
            else
            {
                ShowNotification("参数读取失败");
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"参数读取出错: {ex.Message}");
        }
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
                await ReadParametersAsync(device);
                ShowNotification("参数写入成功");
            }
            else
            {
                ShowNotification("参数写入失败");
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"参数写入出错: {ex.Message}");
        }
    }

    private void InitializeNetworkInterfaces()
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
        var s = _settingsService.Load();
        s.ListenPort = Port;
        s.SelectedInterfaceName = SelectedInterface?.Name ?? string.Empty;
        _settingsService.Save(s);
    }
}