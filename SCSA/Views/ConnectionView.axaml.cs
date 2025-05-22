using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SCSA.IO.Net.TCP;
using SCSA.Models;
using SCSA.ViewModels;

namespace SCSA;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
    }
}