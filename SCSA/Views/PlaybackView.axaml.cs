using Avalonia.Controls;
using Avalonia.ReactiveUI;
using SCSA.ViewModels;

namespace SCSA
{
    public partial class PlaybackView : ReactiveUserControl<PlaybackViewModel>
    {
        public PlaybackView()
        {
            InitializeComponent();
        }
    }
} 