
using Avalonia.Controls;
using SCSA.ViewModels;
using SCSA.Views;

namespace SCSA.Client.Test
{
    public class ViewLocator
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            // 使用显式映射以支持 AOT 编译，避免反射
            switch (data)
            {
                case MainWindowViewModel vm:
                    return new MainWindow { DataContext = vm };
                // 如果还有其他视图模型，请在此添加映射
                default:
                    return new TextBlock { Text = "Not Found: " + data.GetType().FullName };
            }
        }
    }
} 