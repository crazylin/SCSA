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
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using SCSA.Models;

namespace SCSA.ViewModels
{
    // ParameterViewModel.cs
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
                return;

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var parameters = Enum.GetValues<ParameterType>().Select(p => new Parameter() { Address = p }).ToList();
            var result = await ConnectionViewModel.SelectedDevice.DeviceControlApi.ReadParameters(parameters, cts.Token);
            if (result.success)
            {
                SetParameters(result.result);
                ConnectionViewModel.ParameterChanged();
            }
      
        });

        public ICommand SaveCommand => new RelayCommand(async () =>
        {
            if (ConnectionViewModel.SelectedDevice == null)
                return;
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var parameters = new List<Parameter>();
            foreach (var deviceParameter in Config.Categories.SelectMany(c => c.Parameters))
            {
                parameters.Add(new Parameter()
                {
                    Address = (ParameterType)deviceParameter.Address, Length = deviceParameter.DataLength,
                    Value = deviceParameter.Value is bool b ? b ? (byte)1 : (byte)0 : deviceParameter.Value
                });
            }

            var result = await ConnectionViewModel.SelectedDevice.DeviceControlApi.SetParameters(parameters, cts.Token);
            if (result)
            {
               ConnectionViewModel.ParameterChanged();
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

            // 显示对话框并获取用户选择的路径
            var result = await _storageProvider.SaveFilePickerAsync(options);
            if (result == null) return; // 用户取消

            var json = JsonConvert.SerializeObject(Config.Categories, Formatting.Indented);

            File.WriteAllText(result.Path.AbsolutePath, json);
        });
    }

}
