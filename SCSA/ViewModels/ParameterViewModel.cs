using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using SCSA.Models;
using FluentAvalonia.UI.Controls;

namespace SCSA.ViewModels
{

    public class ParameterViewModel : ViewModelBase
    {
        public DeviceConfiguration Config { get; }
        public ObservableCollection<ParameterCategory> Categories { get; } = new();
        public ConnectionViewModel ConnectionViewModel { set; get; }

        private readonly IStorageProvider _storageProvider;

        public ParameterViewModel(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
  
            Config = new DeviceConfiguration();
            Categories = new ObservableCollection<ParameterCategory>(Config.Categories);
        }


        public void SetParameters(List<Parameter> parameters)
        {
            foreach (var deviceParameter in Config.Categories.SelectMany(c=>c.Parameters))
            {
                foreach (var parameter in parameters)
                {
                    if ((int)parameter.Address == deviceParameter.Address)
                    {
                        deviceParameter.Value = parameter.Value;
                        break;
                    }
                }
            }

            ConnectionViewModel.SelectedDevice.DeviceParameters =
                Config.Categories.SelectMany(c => c.Parameters).ToList();
            

        }

        public ICommand ReadParametersFromDeviceCommand => new RelayCommand(async () =>
        {
            if (ConnectionViewModel.SelectedDevice==null)
            {
                ShowNotification("请先选择设备", InfoBarSeverity.Warning);
                return;
            }

            ShowNotification("正在读取参数...", InfoBarSeverity.Informational);
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var parameters = Enum.GetValues<ParameterType>().Select(p => new Parameter() { Address = p }).ToList();
            try
            {
                var result = await ConnectionViewModel.SelectedDevice.DeviceControlApi.ReadParameters(parameters, cts.Token);
                if (result.success)
                {
                    SetParameters(result.result);
                    // 修改事件调用方式
                    ConnectionViewModel.OnParametersChanged(ConnectionViewModel.SelectedDevice);

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
        });

        public ICommand SaveCommand => new RelayCommand(async () =>
        {
            if (ConnectionViewModel.SelectedDevice == null)
            {
                ShowNotification("请先选择设备", InfoBarSeverity.Warning);
                return;
            }

            ShowNotification("正在保存参数...", InfoBarSeverity.Informational);
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var parameters = new List<Parameter>();
            foreach (var deviceParameter in Config.Categories.SelectMany(c => c.Parameters))
            {
                parameters.Add(new Parameter()
                {
                    Address = (ParameterType)deviceParameter.Address, 
                    Length = deviceParameter.DataLength,
                    Value = deviceParameter.Value is bool b ? b ? (byte)1 : (byte)0 : deviceParameter.Value
                });
            }

            try
            {
                var result = await ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(parameters, cts.Token);
                if (result)
                {
                    // 修改为使用新的事件触发方法
                    ConnectionViewModel.OnParametersChanged(ConnectionViewModel.SelectedDevice);
                    ShowNotification("参数保存成功", InfoBarSeverity.Success);
                }
                else
                {
                    ShowNotification("参数保存失败", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"参数保存出错: {ex.Message}", InfoBarSeverity.Error);
            }
        });

        public ICommand SaveAsCommand => new RelayCommand(async () =>
        {
            var options = new FilePickerSaveOptions
            {
                Title = "保存配置文件",
                SuggestedFileName = "data.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("配置文件")
                    {
                        Patterns = new[] { "*.json" }
                    }
                }
            };

            try
            {
                // 显示对话框并获取用户选择的路径
                var result = await _storageProvider.SaveFilePickerAsync(options);
                if (result == null)
                {
                    ShowNotification("已取消保存", InfoBarSeverity.Informational);
                    return;
                }

                var json = JsonConvert.SerializeObject(Config.Categories, Formatting.Indented);
                File.WriteAllText(result.Path.AbsolutePath, json);
                ShowNotification("配置文件保存成功", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowNotification($"保存配置文件失败: {ex.Message}", InfoBarSeverity.Error);
            }
        });
    }
}
