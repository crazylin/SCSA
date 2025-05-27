using Avalonia;
using OxyPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Reactive;

namespace SCSA.Behaviors
{
    public static class PlotViewExtensions
    {
        public static readonly AttachedProperty<bool> EnableCopyMenuProperty =
            AvaloniaProperty.RegisterAttached<PlotView, bool>(
                "EnableCopyMenu", typeof(PlotViewExtensions));

        public static bool GetEnableCopyMenu(PlotView element)
            => element.GetValue(EnableCopyMenuProperty);

        public static void SetEnableCopyMenu(PlotView element, bool value)
            => element.SetValue(EnableCopyMenuProperty, value);

        static PlotViewExtensions()
        {
            EnableCopyMenuProperty.Changed.Subscribe(
                new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(args =>
                {
                    if (args.Sender is PlotView plotView)
                    {
                        bool enabled = args.NewValue.Value as bool? ?? false;

                        if (enabled)
                        {
                            var menuItem = new MenuItem { Header = "复制数据" };
                            menuItem.Click += (s, e) =>
                            {
                                if (plotView.Model is CustomPlotModel model)
                                {
                                    var topLevel = TopLevel.GetTopLevel(plotView);
                                    model.CopyAlignedSeriesDataToClipboard(topLevel);
                                }
                            };

                            // 保留原有菜单项（如果有的话）
                            //var items = plotView.ContextMenu?.Items?? new List<MenuItem>();
                            //items.Insert(0, menuItem);
                            plotView.ContextMenu = new ContextMenu();
                            plotView.ContextMenu.Items.Add(menuItem);
                        }
                        else
                        {
                            // 只移除我们的菜单项（如果有其他菜单项需要保留）
                            if (plotView.ContextMenu is { } menu)
                            {
                                var items = menu.Items.OfType<MenuItem>()
                                    .Where(x => x.Header?.ToString() != "复制数据")
                                    .ToList();
                                foreach (var menuItem in items)
                                {
                                    plotView.ContextMenu.Items.Remove(menuItem);
                                }
                            }
                        }
                    }

                }));
 
        }
    }
}
