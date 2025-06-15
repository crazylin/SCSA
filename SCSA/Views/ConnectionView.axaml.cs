using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class ConnectionView : ReactiveUserControl<ConnectionViewModel>
{
    public ConnectionView()
    {
        InitializeComponent();
    }
}