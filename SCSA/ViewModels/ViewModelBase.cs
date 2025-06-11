using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;
using Avalonia;

namespace SCSA.ViewModels;

public class ViewModelBase : ObservableObject
{
    private TeachingTip _notificationTip;
    protected void ShowNotification(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_notificationTip == null)
            {
                _notificationTip = new TeachingTip
                {
                    Title = "提示",
                    Subtitle = message,
                    IsLightDismissEnabled = true,
                    PreferredPlacement = TeachingTipPlacementMode.Auto,
                    IsOpen = true
                };

                // 获取主窗口并添加通知
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow?.Content is Panel panel)
                    {
                        panel.Children.Add(_notificationTip);
                    }
                }
            }
            else
            {
                _notificationTip.Subtitle = message;
                _notificationTip.IsOpen = true;
            }

            // 3秒后自动关闭
            Task.Delay(3000).ContinueWith(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _notificationTip.IsOpen = false;
                });
            });
        });
    }

}