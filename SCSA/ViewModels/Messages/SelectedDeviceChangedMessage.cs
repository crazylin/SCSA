using SCSA.Models;

namespace SCSA.ViewModels.Messages;

public sealed record SelectedDeviceChangedMessage(DeviceConnection? Value);