using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
    }
}