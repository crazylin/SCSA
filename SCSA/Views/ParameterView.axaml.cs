using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class ParameterView : ReactiveUserControl<ParameterViewModel>
{
    public ParameterView()
    {
        InitializeComponent();
    }
}