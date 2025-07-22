using Avalonia.ReactiveUI;
using ReactiveUI;
using SCSA.ViewModels;

namespace SCSA;

public partial class PulseOutputView : ReactiveUserControl<PulseOutputViewModel>
{
    public PulseOutputView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            // View激活时的逻辑可以在这里添加
        });
    }
}