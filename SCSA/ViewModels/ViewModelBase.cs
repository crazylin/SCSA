using System;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace SCSA.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private TeachingTip _notificationTip;
    public static event Action<string, InfoBarSeverity> NotificationRequested;

    protected void ShowNotification(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        NotificationRequested?.Invoke(message, severity);
    }
}