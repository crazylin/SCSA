using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Models;
using SCSA.ViewModels.Messages;

namespace SCSA.ViewModels;

public class ParameterViewModel : ViewModelBase
{
    private readonly IStorageProvider _storageProvider;

    public ParameterViewModel(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;

        Config = new DeviceConfiguration();
        Categories = new ObservableCollection<ParameterCategory>(Config.Categories);

        // Listen for device and parameter changes
        MessageBus.Current.Listen<SelectedDeviceChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSelectedDeviceChanged);

        MessageBus.Current.Listen<ParametersChangedMessage>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnParametersChanged);

        var canExecute = this.WhenAnyValue(x => x.CurrentDevice)
            .Select(device => device != null);

        ReadParametersFromDeviceCommand = ReactiveCommand.Create(ReadParametersFromDevice, canExecute);
        SaveCommand = ReactiveCommand.Create(Save, canExecute);
        SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAsAsync);
    }

    public DeviceConfiguration Config { get; }
    public ObservableCollection<ParameterCategory> Categories { get; }

    [Reactive] public DeviceConnection? CurrentDevice { get; private set; }

    public ReactiveCommand<Unit, Unit> ReadParametersFromDeviceCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    private void OnSelectedDeviceChanged(SelectedDeviceChangedMessage msg)
    {
        CurrentDevice = msg.Value;
        if (CurrentDevice?.DeviceParameters != null) SetDeviceParameters(CurrentDevice.DeviceParameters);
    }

    private void OnParametersChanged(ParametersChangedMessage msg)
    {
        if (CurrentDevice != null && msg.Value.EndPoint.Equals(CurrentDevice.EndPoint))
            SetDeviceParameters(msg.Value.DeviceParameters);
    }

    private void SetDeviceParameters(List<DeviceParameter> deviceParameters)
    {
        var parameters = deviceParameters
            .Select(dp => new Parameter
                { Address = (ParameterType)dp.Address, Value = dp.Value, Length = dp.DataLength })
            .ToList();
        SetParameters(parameters);
    }

    private void SetParameters(List<Parameter> parameters)
    {
        foreach (var uiParam in Config.Categories.SelectMany(c => c.Parameters))
        {
            var receivedParam = parameters.FirstOrDefault(p => (int)p.Address == uiParam.Address);
            if (receivedParam != null) uiParam.Value = receivedParam.Value;
        }

        if (CurrentDevice != null)
            CurrentDevice.DeviceParameters = Config.Categories.SelectMany(c => c.Parameters).ToList();
    }

    private void ReadParametersFromDevice()
    {
        ShowNotification("正在读取参数...");
        MessageBus.Current.SendMessage(new RequestReadParametersMessage());
    }

    private void Save()
    {
        ShowNotification("正在保存参数...");
        var parameters = Config.Categories.SelectMany(c => c.Parameters)
            .Select(p => new Parameter
            {
                Address = (ParameterType)p.Address,
                Length = p.DataLength,
                Value = p.Value is bool b ? (byte)(b ? 1 : 0) : p.Value
            }).ToList();
        MessageBus.Current.SendMessage(new RequestWriteParametersMessage(parameters));
    }

    private async Task SaveAsAsync()
    {
        var options = new FilePickerSaveOptions
        {
            Title = "保存配置文件",
            SuggestedFileName = "data.json",
            FileTypeChoices = new[] { new FilePickerFileType("配置文件") { Patterns = new[] { "*.json" } } }
        };

        try
        {
            var result = await _storageProvider.SaveFilePickerAsync(options);
            if (result == null)
            {
                ShowNotification("已取消保存");
                return;
            }

            var json = JsonSerializer.Serialize(Config.Categories, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(result.Path.AbsolutePath, json);
            ShowNotification("配置文件保存成功", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification($"保存配置文件失败: {ex.Message}", InfoBarSeverity.Error);
        }
    }
}