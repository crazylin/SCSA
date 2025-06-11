using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using SCSA.Models;
using SCSA.IO.Net.TCP;
using SCSA.Services.Device;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using FluentAvalonia.UI.Controls;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SCSA.ViewModels
{
    public partial class ConnectionViewModel : ViewModelBase
    {
        // 修改为 public 访问级别
        public event EventHandler<DeviceConnection>? ParametersChanged;

        // 添加事件触发方法
        public void OnParametersChanged(DeviceConnection device)
        {
            ParametersChanged?.Invoke(this, device);
        }

        private readonly PipelineTcpServer<PipelineNetDataPackage> _tcpServer;
        public ObservableCollection<NetworkInterfaceInfo> NetworkInterfaces { set; get; }
        public NetworkInterfaceInfo SelectedInterface { get; set; }

        [ObservableProperty]
        // 端口设置
        private int _port = 9123;
        
        // 已连接设备
        public ObservableCollection<DeviceConnection> ConnectedDevices { get; } = new();

        private DeviceConnection _selectedDevice;


        public DeviceConnection SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    if (_selectedDevice != null)
                    {
                        ShowNotification($"已选中设备: {_selectedDevice.DeviceId}");
                    }
                }
            }
        }

        private string _statusMessage = "准备就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }


        public ParameterViewModel ParameterViewModel { set; get; } 
        public ConnectionViewModel(PipelineTcpServer<PipelineNetDataPackage> tcpServer, ParameterViewModel parameterViewModel)
        {
            _tcpServer = tcpServer;
            ParameterViewModel = parameterViewModel;
            _tcpServer.ClientConnected += _tcpServer_ClientConnected;
            _tcpServer.ClientDisconnected += _tcpServer_ClientDisconnected;
            InitializeNetworkInterfaces();

            ParameterViewModel.ConnectionViewModel = this;
        }

        private void _tcpServer_ClientDisconnected(object? sender, PipelineTcpClient<PipelineNetDataPackage> e)
        {
            Log.Debug($"Client Disconnected {e.RemoteEndPoint}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var device = ConnectedDevices.FirstOrDefault(d => Equals(d.EndPoint, e.RemoteEndPoint));
                if (device != null)
                {
                    ConnectedDevices.Remove(device);
                    ShowNotification($"设备已断开连接: {e.RemoteEndPoint}", InfoBarSeverity.Warning);
                }
            });
        }

        private void _tcpServer_ClientConnected(object? sender, PipelineTcpClient<PipelineNetDataPackage> e)
        {
            Log.Debug($"Client Connected {e.RemoteEndPoint}");

            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
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
                ShowNotification($"新设备已连接: {e.RemoteEndPoint}", InfoBarSeverity.Success);

                if (SelectedDevice == null && ConnectedDevices.Count > 0)
                {
                    SelectedDevice = device;
                    // 连接后立即读取参数
                    await ReadParametersAsync(device);
                }
            });
        }

        private async Task ReadParametersAsync(DeviceConnection device)
        {
            if (device == null)
                return;

            ShowNotification("正在读取参数...", InfoBarSeverity.Informational);
            
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var parameters = ParameterViewModel.Categories.SelectMany(c => c.Parameters.Select(p => p.Address))
                .Select(p => new Parameter() { Address = (ParameterType)p }).ToList();
            
            try 
            {
                var result = await device.DeviceControlApi.ReadParameters(parameters, cts.Token);
                if (result.success)
                {
                    ParameterViewModel.SetParameters(result.result);
                    // 修改事件调用方式
                    ParametersChanged?.Invoke(this, device);
                    ShowNotification("参数读取成功", InfoBarSeverity.Success);
                }
                else
                {
                    ShowNotification("参数读取失败", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"参数读取出错: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void InitializeNetworkInterfaces()
        {
            var physicalInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => /*ni.OperationalStatus == OperationalStatus.Up &&*/
                             IsPhysicalInterface(ni)) // 过滤物理网卡
                .Select(ni => new NetworkInterfaceInfo
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    IPAddress = GetIpAddress(ni),
                    RawInterface = ni
                })
                .OrderBy(ni => ni.Name);

            NetworkInterfaces = new ObservableCollection<NetworkInterfaceInfo>(physicalInterfaces);
            SelectedInterface = NetworkInterfaces.FirstOrDefault();
        }
        private string GetIpAddress(NetworkInterface networkInterface)
        {
            var ipProperties = networkInterface.GetIPProperties();
            var ipv4Address = ipProperties.UnicastAddresses
                .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

            return ipv4Address?.ToString() ?? "无 IP 地址";
        }
        private bool IsPhysicalInterface(NetworkInterface networkInterface)
        {
            return networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                   networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                   networkInterface.NetworkInterfaceType == NetworkInterfaceType.Fddi;
        }

        public ICommand StartCommand => new RelayCommand(() =>
        {
            if (SelectedInterface == null)
            {
                ShowNotification("请选择网络接口", InfoBarSeverity.Error);
                return;
            }

            try
            {
                var endPoint = GetEndPoint(SelectedInterface, Port);
                _tcpServer.Start(endPoint);
                ShowNotification($"正在监听 {SelectedInterface.Name}:{Port}", InfoBarSeverity.Success);
                Log.Debug("Server Started");
            }
            catch (Exception ex)
            {
                ShowNotification($"启动失败: {ex.Message}", InfoBarSeverity.Error);
                Log.Debug($"Server Started Error {ex.Message}");
            }
        });

        public ICommand StopCommand => new RelayCommand(() =>
        {
            _tcpServer.Stop();
            ShowNotification("已停止监听", InfoBarSeverity.Informational);
            Log.Debug("Server Stopped");
        });

        public ICommand DisconnectCommand => new RelayCommand<DeviceConnection>(device =>
        {
            device.Client.Close();
            ConnectedDevices.Remove(device);
            ShowNotification($"已断开设备连接: {device.EndPoint}", InfoBarSeverity.Informational);
        });

        public IPEndPoint GetEndPoint(NetworkInterfaceInfo networkInterface, int port)
        {
            if (networkInterface == null)
            {
                throw new ArgumentNullException(nameof(networkInterface), "网络接口不能为空");
            }

            // 获取网络接口的 IP 配置
            var ipProperties = networkInterface.RawInterface.GetIPProperties();

            // 获取 IPv4 地址
            var ipv4Address = ipProperties.UnicastAddresses
                .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

            if (ipv4Address == null)
            {
                throw new InvalidOperationException("未找到有效的 IPv4 地址");
            }

            // 组合 IP 地址和端口
            return new IPEndPoint(ipv4Address, port);
        }


        public ICommand SelectDeviceCommand => new RelayCommand<DeviceConnection>(device =>
        {
            if (device != null)
            {
                SelectedDevice = device;
                ReadParametersAsync(device);
            }
        });

        public AsyncRelayCommand<DeviceConnection> ReadParameterCommand => new AsyncRelayCommand<DeviceConnection>(
            async device => await ReadParametersAsync(device));
    }
}
