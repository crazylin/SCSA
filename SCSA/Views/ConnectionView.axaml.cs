using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SCSA.Models;
using SCSA.ViewModels;

namespace SCSA;

public partial class ConnectionView : ReactiveUserControl<ConnectionViewModel>
{
    public ConnectionView()
    {
        InitializeComponent();
    }

}