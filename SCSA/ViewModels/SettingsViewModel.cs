using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SCSA.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;
using SCSA.Models;
using System.Text.Json;

namespace SCSA.ViewModels
{
    public enum StorageType
    {
        ByLength,
        ByTime
    }

    public partial class SettingsViewModel : ViewModelBase
    {
        private const string CONFIG_FILE = "appsettings.json";
        private readonly string _configPath;

        [ObservableProperty] private bool _enableDataStorage = true;
        [ObservableProperty]
        private string _dataStoragePath;

        [ObservableProperty] private int _dataLength = 64 * 1024 * 1024;
        [ObservableProperty]
        private ObservableCollection<TriggerType> _triggerTypes;

        [ObservableProperty]
        private TriggerType _selectedTriggerType;

        [ObservableProperty]
        private string _dataLengthDisplay;

        [ObservableProperty]
        private StorageType _selectedStorageType = StorageType.ByLength;

        [ObservableProperty]
        private int _storageTime = 5; // 默认5秒

        [ObservableProperty]
        private string _storageTimeDisplay;

        [ObservableProperty]
        private bool _showDataLengthSettings = true;

        public ObservableCollection<StorageType> StorageTypes { get; } = new(Enum.GetValues<StorageType>());

        public SettingsViewModel()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
            TriggerTypes =
                new ObservableCollection<TriggerType>(new[] { TriggerType.FreeTrigger, TriggerType.DebugTrigger });
            
            LoadSettings();
            
            // 订阅属性变更事件
            this.PropertyChanged += SettingsViewModel_PropertyChanged;
        }

        private void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DataLengthDisplay) && 
                e.PropertyName != nameof(StorageTimeDisplay))
            {
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    if (settings != null)
                    {
                        EnableDataStorage = settings.EnableDataStorage;
                        DataStoragePath = settings.DataStoragePath;
                        DataLength = settings.DataLength;
                        SelectedStorageType = settings.SelectedStorageType;
                        StorageTime = settings.StorageTime;
                        SelectedTriggerType = settings.SelectedTriggerType;
                    }
                }
                else
                {
                    // 如果配置文件不存在，使用默认值
                    DataStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SCSA");
                }
            }
            catch (Exception ex)
            {
                // 如果加载失败，使用默认值
                DataStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SCSA");
            }

            UpdateDataLengthDisplay();
            UpdateStorageTimeDisplay();
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    EnableDataStorage = EnableDataStorage,
                    DataStoragePath = DataStoragePath,
                    DataLength = DataLength,
                    SelectedStorageType = SelectedStorageType,
                    StorageTime = StorageTime,
                    SelectedTriggerType = SelectedTriggerType
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception)
            {
                // 保存失败时不处理，避免影响用户体验
            }
        }

        partial void OnDataLengthChanged(int value)
        {
            UpdateDataLengthDisplay();
        }

        partial void OnStorageTimeChanged(int value)
        {
            UpdateStorageTimeDisplay();
        }

        partial void OnSelectedStorageTypeChanged(StorageType value)
        {
            // 当存储类型改变时，更新显示
            UpdateDataLengthDisplay();
            UpdateStorageTimeDisplay();
        }

        partial void OnSelectedTriggerTypeChanged(TriggerType value)
        {
            ShowDataLengthSettings = value != TriggerType.DebugTrigger;
            UpdateDataLengthDisplay();
        }

        private void UpdateDataLengthDisplay()
        {
            if (SelectedStorageType != StorageType.ByLength)
            {
                DataLengthDisplay = string.Empty;
                return;
            }

            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = DataLength;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            DataLengthDisplay = $"{len:0.##} {sizes[order]}";
        }

        private void UpdateStorageTimeDisplay()
        {
            if (SelectedStorageType != StorageType.ByTime)
            {
                StorageTimeDisplay = string.Empty;
                return;
            }

            if (StorageTime < 60)
            {
                StorageTimeDisplay = $"{StorageTime} 秒";
            }
            else if (StorageTime < 3600)
            {
                StorageTimeDisplay = $"{StorageTime / 60} 分钟 {StorageTime % 60} 秒";
            }
            else
            {
                int hours = StorageTime / 3600;
                int minutes = (StorageTime % 3600) / 60;
                int seconds = StorageTime % 60;
                StorageTimeDisplay = $"{hours} 小时 {minutes} 分钟 {seconds} 秒";
            }
        }

        [RelayCommand]
        private async Task BrowseStoragePath()
        {
            var topLevel = Avalonia.Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (topLevel == null) return;

            var folders = await topLevel.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择数据存储位置",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                DataStoragePath = folders[0].Path.LocalPath;
            }
        }
    }
}
