using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class FirmwareUpdateView : ReactiveUserControl<FirmwareUpdateViewModel>
{
    public FirmwareUpdateView()
    {
        InitializeComponent();
    }
}