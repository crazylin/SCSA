using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SCSA.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCSA.ViewModels
{
    public partial class FirmwareUpdateViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _currentVersion = "1.0.0";
        [ObservableProperty]
        private string _newVersion = "1.1.0";
        [ObservableProperty]
        private double _progressPercentage;
        [ObservableProperty]
        private double _maxPercentage = 100;
        [ObservableProperty]
        private string _statusMessage = "准备就绪";
        [ObservableProperty]
        private bool _canExecute = true;
        [ObservableProperty]
        private string _buttonText = "开始升级";
        [ObservableProperty]
        private string _selectedFilePath;
        [ObservableProperty]
        private bool _controlEnable;


        private CancellationTokenSource _cts;

        public ConnectionViewModel ConnectionViewModel { set; get; }

 

        public FirmwareUpdateViewModel(ConnectionViewModel connectionViewModel)
        {
            ConnectionViewModel = connectionViewModel;
            connectionViewModel.PropertyChanged += ConnectionViewModel_PropertyChanged;
        }

        private void ConnectionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectionViewModel.SelectedDevice))
            {
                ControlEnable = ConnectionViewModel.SelectedDevice != null;

            }
        }

        public byte[] FirmwareData { set; get; }



        // 命令
        public ICommand StartUpgradeCommand => new RelayCommand(async () =>
        {
            try
            {
                if (ButtonText == "取消升级")
                {
                    _cts.Cancel(true);
                    return;
                }
                if (ConnectionViewModel.SelectedDevice == null)
                {
                    StatusMessage = "请先连接设备";
                    return;
                }
                if (string.IsNullOrEmpty(SelectedFilePath))
                {
                    StatusMessage = "请先选择固件文件";
                    return;
                }
              
                ButtonText = "取消升级";

                _cts = new CancellationTokenSource();
                await Task.Run( async () => await PerformFirmwareUpdate(_cts.Token));
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "升级已取消";
            }
            finally
            {
           
                ButtonText = "开始升级";
      
            }
        });

        private async Task PerformFirmwareUpdate(CancellationToken token)
        {

            if (!await ConnectionViewModel.SelectedDevice.DeviceControlApi.FirmwareUpgradeStart(FirmwareData.Length,
                    new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
            {
                StatusMessage = "开始升级失败！";
                return;
            }
            var perDataLength = 1280;
            for (int i = 0; i <= FirmwareData.Length; i += perDataLength)
            {
                token.ThrowIfCancellationRequested();

                ProgressPercentage = i;
                StatusMessage = $"升级中... {i}/{MaxPercentage}";
                var datas = FirmwareData.Skip(i).Take(perDataLength).ToArray();
                var packageId = i / perDataLength + 1;
                if (!await ConnectionViewModel.SelectedDevice.DeviceControlApi.FirmwareUpgradeTransfer(packageId, datas,
                        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token))
                {
                    StatusMessage = "数据传输异常！";
                    return;
                }
            }

            StatusMessage = "升级完成！";
        }



        // 修改后的浏览命令（使用强类型参数）
        public ICommand BrowseCommand => new RelayCommand<Control>(async (control) =>
        {
            if (control == null) return;

            var topLevel = TopLevel.GetTopLevel(control);
            var storageProvider = topLevel?.StorageProvider;

            if (storageProvider != null)
            {
                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("固件文件")
                        {
                            Patterns = new[] { "*.bin", "*.hex" }
                        }
                    }
                });

                if (files.Count > 0 && !string.IsNullOrWhiteSpace(files[0].TryGetLocalPath()))
                {
                    SelectedFilePath = files[0].TryGetLocalPath();
                    NewVersion = ParseFirmwareVersion(SelectedFilePath);
                }
            }
        });

        // 新增文件版本解析方法
        private string ParseFirmwareVersion(string path)
        {
            try
            {
                // 示例解析逻辑，实际需要根据文件格式实现
                var fileName = Path.GetFileNameWithoutExtension(path);
                FirmwareData = File.ReadAllBytes(path);
                MaxPercentage = FirmwareData.Length;
                return fileName.Split('_').LastOrDefault() ?? "未知版本";
            }
            catch
            {
                return "版本解析失败";
            }
        }
    }
}
