using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SCSA.Models;
using SCSA.Services;
using SCSA.ViewModels.Messages;

namespace SCSA.ViewModels;

public enum StorageType
{
    Time,
    Length
}

public enum FileFormatType
{
    Binary,
    UFF,
    WAV
}

public enum UFFFormatType
{
    ASCII,
    Binary
}

public class SettingsViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<string> _dataLengthDisplay;
    private readonly IAppSettingsService _settingsService;

    private readonly ObservableAsPropertyHelper<string> _storageTimeDisplay;

    [Reactive] public int DataLength { get; set; } = 102400;

    [Reactive] public string DataStoragePath { get; set; }

    [Reactive] public bool EnableDataStorage { get; set; } = true;

    [Reactive] public FileFormatType SelectedFileFormat { get; set; } = FileFormatType.UFF;

    [Reactive] public StorageType SelectedStorageType { get; set; } = StorageType.Length;

    [Reactive] public TriggerType SelectedTriggerType { get; set; } = TriggerType.FreeTrigger;

    [Reactive] public UFFFormatType SelectedUFFFormat { get; set; } = UFFFormatType.Binary;

    [Reactive] public bool ShowDataLengthSettings { get; set; } = true;

    [Reactive] public bool ShowUFFFormatSettings { get; set; }

    [Reactive] public int StorageTime { get; set; } = 10;

    [Reactive] public ObservableCollection<TriggerType> TriggerTypes { get; set; }

    [Reactive] public bool EnableLogging { get; set; } = true;

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;

        StorageTypes = new ObservableCollection<StorageType>(Enum.GetValues<StorageType>());
        TriggerTypes =
            new ObservableCollection<TriggerType>(new[] { TriggerType.FreeTrigger, TriggerType.DebugTrigger });
        FileFormatTypes = new ObservableCollection<FileFormatType>(Enum.GetValues<FileFormatType>());
        UFFFormatTypes = new ObservableCollection<UFFFormatType>(Enum.GetValues<UFFFormatType>());

        LoadSettings();

        SaveSettingsCommand = ReactiveCommand.Create(SaveSettings);

        // Any time a property changes, we save the settings.
        this.WhenAnyValue(
            x => x.EnableDataStorage,
            x => x.SelectedStorageType,
            x => x.StorageTime,
            x => x.DataLength,
            x => x.SelectedTriggerType,
            x => x.SelectedFileFormat,
            x => x.SelectedUFFFormat
        ).Subscribe(_ => SaveSettingsCommand.Execute().Subscribe());

        // 监听日志开关单独保存，避免 WhenAnyValue 参数数量超限导致编译错误
        this.WhenAnyValue(x => x.EnableLogging)
            .Subscribe(_ => SaveSettingsCommand.Execute().Subscribe());

        // reactive derived displays
        this.WhenAnyValue(x => x.SelectedStorageType, x => x.DataLength)
            .Select(t => FormatDataLength(t.Item1, t.Item2))
            .ToProperty(this, x => x.DataLengthDisplay, out _dataLengthDisplay, scheduler: RxApp.MainThreadScheduler);

        this.WhenAnyValue(x => x.SelectedStorageType, x => x.StorageTime)
            .Select(t => FormatStorageTime(t.Item1, t.Item2))
            .ToProperty(this, x => x.StorageTimeDisplay, out _storageTimeDisplay, scheduler: RxApp.MainThreadScheduler);

        // 更新 ShowDataLengthSettings: 调试触发时隐藏数据长度设置
        this.WhenAnyValue(x => x.SelectedTriggerType)
            .Subscribe(trigger => ShowDataLengthSettings = trigger != TriggerType.DebugTrigger);

        // 初始化 ShowDataLengthSettings 值
        ShowDataLengthSettings = SelectedTriggerType != TriggerType.DebugTrigger;

        BrowseStoragePathCommand = ReactiveCommand.CreateFromTask(BrowseStoragePath);
    }

    public ObservableCollection<StorageType> StorageTypes { get; }
    public ObservableCollection<FileFormatType> FileFormatTypes { get; }
    public ObservableCollection<UFFFormatType> UFFFormatTypes { get; }

    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseStoragePathCommand { get; }

    public string DataLengthDisplay => _dataLengthDisplay.Value;

    public string StorageTimeDisplay => _storageTimeDisplay.Value;

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        EnableDataStorage = settings.EnableDataStorage;
        DataStoragePath = settings.DataStoragePath;
        DataLength = settings.DataLength;
        SelectedStorageType = settings.SelectedStorageType;
        StorageTime = (int)settings.StorageTime;
        SelectedTriggerType = settings.SelectedTriggerType;
        SelectedFileFormat = settings.SelectedFileFormat;
        SelectedUFFFormat = settings.SelectedUFFFormat;
        EnableLogging = settings.EnableLogging;

        // triggers calculations immediately
        this.RaisePropertyChanged(nameof(DataLengthDisplay));
        this.RaisePropertyChanged(nameof(StorageTimeDisplay));
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            EnableDataStorage = EnableDataStorage,
            DataStoragePath = DataStoragePath,
            DataLength = DataLength,
            SelectedStorageType = SelectedStorageType,
            StorageTime = StorageTime,
            SelectedTriggerType = SelectedTriggerType,
            SelectedFileFormat = SelectedFileFormat,
            SelectedUFFFormat = SelectedUFFFormat,
            EnableLogging = this.EnableLogging
        };

        _settingsService.Save(settings);
        MessageBus.Current.SendMessage(new SettingsChangedMessage(settings));
    }

    private static string FormatDataLength(StorageType type, int dataLength)
    {
        if (type != StorageType.Length) return string.Empty;
        string[] sizes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double len = dataLength;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatStorageTime(StorageType type, int storageTime)
    {
        if (type != StorageType.Time) return string.Empty;
        if (storageTime < 60)
            return $"{storageTime:0.##} 秒";
        if (storageTime < 3600)
        {
            var minutes = storageTime / 60;
            double seconds = storageTime % 60;
            return seconds > 0 ? $"{minutes} 分钟 {seconds:0.##} 秒" : $"{minutes} 分钟";
        }

        var hrs = storageTime / 3600;
        var mins = storageTime % 3600 / 60;
        double secs = storageTime % 60;
        if (secs > 0) return $"{hrs} 小时 {mins} 分钟 {secs:0.##} 秒";
        return mins > 0 ? $"{hrs} 小时 {mins} 分钟" : $"{hrs} 小时";
    }

    private async Task BrowseStoragePath()
    {
        var topLevel = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (topLevel == null) return;

        var folders = await topLevel.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择数据存储位置",
            AllowMultiple = false
        });

        if (folders.Count > 0) DataStoragePath = folders[0].Path.LocalPath;
    }
}