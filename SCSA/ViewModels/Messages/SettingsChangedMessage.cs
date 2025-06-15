using SCSA.Models;

namespace SCSA.ViewModels.Messages;

public sealed record SettingsChangedMessage(AppSettings Value);