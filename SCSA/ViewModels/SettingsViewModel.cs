using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
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

    private int _dataLength = 102400;

    private string _dataStoragePath;

    private bool _enableDataStorage = true;

    private FileFormatType _selectedFileFormat = FileFormatType.UFF;

    private StorageType _selectedStorageType = StorageType.Length;

    private TriggerType _selectedTriggerType = TriggerType.FreeTrigger;

    private UFFFormatType _selectedUFFFormat = UFFFormatType.Binary;

    private bool _showDataLengthSettings = true;

    private bool _showUFFFormatSettings;

    private int _storageTime = 10;

    private ObservableCollection<TriggerType> _triggerTypes;

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

        // reactive derived displays
        this.WhenAnyValue(x => x.SelectedStorageType, x => x.DataLength)
            .Select(t => FormatDataLength(t.Item1, t.Item2))
            .ToProperty(this, x => x.DataLengthDisplay, out _dataLengthDisplay, scheduler: RxApp.MainThreadScheduler);

        this.WhenAnyValue(x => x.SelectedStorageType, x => x.StorageTime)
            .Select(t => FormatStorageTime(t.Item1, t.Item2))
            .ToProperty(this, x => x.StorageTimeDisplay, out _storageTimeDisplay, scheduler: RxApp.MainThreadScheduler);
    }

    public bool EnableDataStorage
    {
        get => _enableDataStorage;
        set => this.RaiseAndSetIfChanged(ref _enableDataStorage, value);
    }

    public string DataStoragePath
    {
        get => _dataStoragePath;
        set => this.RaiseAndSetIfChanged(ref _dataStoragePath, value);
    }

    public int DataLength
    {
        get => _dataLength;
        set => this.RaiseAndSetIfChanged(ref _dataLength, value);
    }

    public ObservableCollection<TriggerType> TriggerTypes
    {
        get => _triggerTypes;
        set => this.RaiseAndSetIfChanged(ref _triggerTypes, value);
    }

    public TriggerType SelectedTriggerType
    {
        get => _selectedTriggerType;
        set => this.RaiseAndSetIfChanged(ref _selectedTriggerType, value);
    }

    public string DataLengthDisplay => _dataLengthDisplay.Value;

    public StorageType SelectedStorageType
    {
        get => _selectedStorageType;
        set => this.RaiseAndSetIfChanged(ref _selectedStorageType, value);
    }

    public int StorageTime
    {
        get => _storageTime;
        set => this.RaiseAndSetIfChanged(ref _storageTime, value);
    }

    public string StorageTimeDisplay => _storageTimeDisplay.Value;

    public bool ShowDataLengthSettings
    {
        get => _showDataLengthSettings;
        set => this.RaiseAndSetIfChanged(ref _showDataLengthSettings, value);
    }

    public FileFormatType SelectedFileFormat
    {
        get => _selectedFileFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedFileFormat, value);
    }

    public UFFFormatType SelectedUFFFormat
    {
        get => _selectedUFFFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedUFFFormat, value);
    }

    public bool ShowUFFFormatSettings
    {
        get => _showUFFFormatSettings;
        set => this.RaiseAndSetIfChanged(ref _showUFFFormatSettings, value);
    }

    public ObservableCollection<StorageType> StorageTypes { get; }
    public ObservableCollection<FileFormatType> FileFormatTypes { get; }
    public ObservableCollection<UFFFormatType> UFFFormatTypes { get; }

    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>
    ///     获取是否保存为UFF格式
    /// </summary>
    public bool SaveAsUFF => SelectedFileFormat == FileFormatType.UFF;

    /// <summary>
    ///     获取是否使用二进制UFF格式
    /// </summary>
    public bool UseBinaryUFF => SelectedFileFormat == FileFormatType.UFF && SelectedUFFFormat == UFFFormatType.Binary;

    /// <summary>
    ///     获取文件扩展名
    /// </summary>
    public string FileExtension => SelectedFileFormat switch
    {
        FileFormatType.UFF => "uff",
        FileFormatType.WAV => "wav",
        _ => "bin"
    };

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
        ShowUFFFormatSettings = settings.SelectedFileFormat == FileFormatType.UFF;

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
            SelectedUFFFormat = SelectedUFFFormat
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