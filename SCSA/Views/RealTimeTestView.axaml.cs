using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class RealTimeTestView : ReactiveUserControl<RealTimeTestViewModel>
{
    public RealTimeTestView()
    {
        InitializeComponent();
    }
}