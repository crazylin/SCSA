using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace SCSA.ViewModels;

public static class BoolToTextConverter
{
    public static readonly IValueConverter ConnectedStatus = new FuncValueConverter<bool, string>(
        connected => connected ? "已连接" : "未连接");
        
    public static readonly IValueConverter RunningStatus = new FuncValueConverter<bool, string>(
        running => running ? "运行中" : "已停止");
}