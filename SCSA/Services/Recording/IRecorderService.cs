using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SCSA.Models;
using SCSA.ViewModels;

namespace SCSA.Services.Recording;

public interface IRecorderService
{
    string StoragePath { get; set; }

    Task<long> StartRecordingAsync(DataChannelType signalType, TriggerType triggerType,
        double sampleRate, IProgress<int> saveProgress, long targetDataLength = 0,
        double targetTimeSeconds = 0,
        StorageType storageType = StorageType.Length, FileFormatType fileFormat = FileFormatType.Binary,
        bool useBinaryUFF = true);

    Task StopRecordingAsync();

    Task<bool> WriteDataAsync(Dictionary<DataChannelType, double[,]> channelDatas);

    //void RecordCmdId(short cmdId);

    event EventHandler DataCollectionCompleted;
}