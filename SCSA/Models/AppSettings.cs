using SCSA.ViewModels;

namespace SCSA.Models;

public class AppSettings
{
    public bool EnableDataStorage { get; set; } = false;
    public string DataStoragePath { get; set; }
    public int DataLength { get; set; } = 64 * 1024 * 1024 / 4;
    public StorageType SelectedStorageType { get; set; } = StorageType.Length;
    public double StorageTime { get; set; } = 5.0;
    public TriggerType SelectedTriggerType { get; set; } = TriggerType.FreeTrigger;
    public FileFormatType SelectedFileFormat { get; set; } = FileFormatType.Binary;
    public UFFFormatType SelectedUFFFormat { get; set; } = UFFFormatType.Binary;

    // Network settings
    public int ListenPort { get; set; } = 9123;
    public string SelectedInterfaceName { get; set; } = string.Empty;

    public bool EnableLogging { get; set; } = true;
}