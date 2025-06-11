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

namespace SCSA.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        [ObservableProperty] private bool _enableDataStorage = true;
        [ObservableProperty]
        private string _dataStoragePath;

        [ObservableProperty] private int _dataLength = 64 * 1024 * 1024;
        [ObservableProperty]
        private ObservableCollection<TriggerType> _triggerTypes;

        [ObservableProperty]
        private TriggerType _selectedTriggerType;



        public SettingsViewModel()
        {
            TriggerTypes =
                new ObservableCollection<TriggerType>(new[] { TriggerType.FreeTrigger, TriggerType.DebugTrigger });
            SelectedTriggerType = TriggerType.FreeTrigger;
            DataStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SCSA");
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
