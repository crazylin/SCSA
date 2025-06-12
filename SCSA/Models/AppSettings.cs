using System;
using System.Text.Json.Serialization;
using SCSA.ViewModels;

namespace SCSA.Models
{
    public class AppSettings
    {
        public bool EnableDataStorage { get; set; } = false;
        public string DataStoragePath { get; set; }
        public int DataLength { get; set; } = 64 * 1024 * 1024 / 4;
        public StorageType SelectedStorageType { get; set; } = StorageType.ByLength;
        public int StorageTime { get; set; } = 5;
        public TriggerType SelectedTriggerType { get; set; } = TriggerType.FreeTrigger;
    }
} 