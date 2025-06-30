using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class DebugParameterView : ReactiveUserControl<DebugParameterViewModel>
{
    public DebugParameterView()
    {
        InitializeComponent();
    }
} 