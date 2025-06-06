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
using System.Net.Sockets;
using System.Net;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace SCSA.ViewModels
{
    public partial class ConnectionViewModel : ViewModelBase
    {
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
                    // 处理选中设备后的逻辑
                    if (_selectedDevice != null)
                    {
                        ReadParameterCommand.ExecuteAsync(_selectedDevice);
                        StatusMessage = $"已选中设备: {_selectedDevice.DeviceId}";
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
                }
            });
        }

        private void _tcpServer_ClientConnected(object? sender, PipelineTcpClient<PipelineNetDataPackage> e)
        {

            Log.Debug($"Client Connected {e.RemoteEndPoint}");

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ConnectedDevices.Add(new DeviceConnection
                {
                    //DeviceId = string.Empty,
                    //FirmwareVersion = string.Empty,
                    EndPoint = e.RemoteEndPoint,
                    ConnectTime = DateTime.Now,
                    Client = e,
                    DeviceParameters = new List<DeviceParameter>(),
                    DeviceControlApi = new PipelineDeviceControlApiAsync(e)
                });
                //e.Start();

                if (SelectedDevice == null && ConnectedDevices.Count > 0)
                    SelectedDevice = ConnectedDevices.First();
            });
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
                StatusMessage = "请选择网络接口";
                return;
            }

            try
            {

                var endPoint = GetEndPoint(SelectedInterface, Port);
                _tcpServer.Start(endPoint);
                StatusMessage = $"正在监听 {SelectedInterface.Name}:{Port}";

                Log.Debug("Server Started");
            }
            catch (Exception ex)
            {
                StatusMessage = $"启动失败: {ex.Message}";


                Log.Debug($"Server Started Error {ex.Message}");
            }
        });

        public ICommand StopCommand => new RelayCommand(() =>
        {
            _tcpServer.Stop();
            StatusMessage = "已停止监听";

            Log.Debug("Server Stopped");
        });

        public ICommand DisconnectCommand => new RelayCommand<DeviceConnection>(device =>
        {
            device.Client.Close();
            ConnectedDevices.Remove(device);
            //_tcpServer.DisconnectDevice(device.DeviceId);

        });

        public AsyncRelayCommand<DeviceConnection> ReadParameterCommand => new AsyncRelayCommand<DeviceConnection>(async device =>
        {
            if(device==null)
                return;
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var parameters = Enum.GetValues<ParameterType>().Select(p => new Parameter() { Address = p }).ToList();
            var result = await device.DeviceControlApi.ReadParameters(parameters, cts.Token);
            if (result.success)
            {
                ParameterViewModel.SetParameters(result.result);
                ParameterChanged();
            }

         
            //await device.DeviceControlApi.SetParameters(new List<Parameter>() { new Parameter() { Address = 0x00000000, Length = 0x01, Value = (byte)1 } }, cts.Token);
        });


        public void ParameterChanged()
        {
            OnPropertyChanged(nameof(SelectedDevice));
        }


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

    }
}
