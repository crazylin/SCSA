using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.Styling;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Axes;
using OxyPlot.Series;
using SCSA.Utils;
using OxyPlot.Annotations;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using FluentAvalonia.UI.Media;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;




namespace SCSA
{
    public class CustomPlotModel : PlotModel
    {

        private bool _selectionEnabled;
        private bool _zeroToggled;
        private bool _isLogScale = false;
        private double _selectStart = double.NaN;
        private RectangleAnnotation _selectRect;
        private TextAnnotation _resultAnnotation;
        private IconButtonAnnotation _btnCopy;
        private IconButtonAnnotation _btnSelect;
        private IconButtonAnnotation _btnLogToggle;
        private Dictionary<Axis, double?> _originalMin = new Dictionary<Axis, double?>();



        public CustomPlotModel()
        {
            Application.Current.ActualThemeVariantChanged += (s, e) => ApplyTheme();

            // 加载图标
            //var zeroImg = new OxyImage(File.ReadAllBytes(zeroIconPath));
            //var selectImg = new OxyImage(File.ReadAllBytes(selectIconPath));
            // 初始化按钮
            _btnCopy = new IconButtonAnnotation
                { Label = "Copy", Width = 36, ClickAction = ToggleCopy, Layer = AnnotationLayer.AboveSeries };
            _btnSelect = new IconButtonAnnotation
                { Label = "SEL", Width = 36, ClickAction = ToggleSelection, Layer = AnnotationLayer.AboveSeries };
            _btnLogToggle = new IconButtonAnnotation
            { Label = "LOG", Width = 36, ClickAction = ToggleLogScale, Layer = AnnotationLayer.AboveSeries };

            Annotations.Add(_btnCopy);
            Annotations.Add(_btnSelect);
            Annotations.Add(_btnLogToggle);

        }


        protected override void OnMouseUp(object sender, OxyMouseEventArgs e)
        {
            base.OnMouseUp(sender, e);
            var sp = e.Position;
            // 检测图标按钮点击
            foreach (var btn in new[] { _btnCopy, _btnSelect, _btnLogToggle })
            {
                if (btn.ScreenRectangle.Contains(sp))
                {
                    e.Handled = true; // 防止事件进一步传播
                    return;
                }
            }

            // 如果处于框选模式并且鼠标左键抬起，结束框选并计算最大值
            if (_selectionEnabled && _selectRect != null && !double.IsNaN(_selectStart))
            {
                _selectStart = double.NaN;

                var minX = _selectRect.MinimumX;
                var maxX = _selectRect.MaximumX;

                double bestY = double.MinValue, bestX = 0;
                foreach (var ls in Series.OfType<LineSeries>())
                foreach (var pt in ls.ItemsSource as IEnumerable<DataPoint>)
                {
                    if (pt.X >= minX && pt.X <= maxX && pt.Y > bestY)
                    {
                        bestY = pt.Y;
                        bestX = pt.X;
                    }
                }

                if (_resultAnnotation == null)
                {
                    // 显示结果
                    _resultAnnotation = new TextAnnotation
                    {
                        Text = $"Max: x={bestX:0.######} y={bestY:0.######}",
                        TextPosition = new DataPoint(bestX, bestY),
                        FontSize = 12,
                        Layer = AnnotationLayer.AboveSeries,
                        TextColor = this.TextColor,
                        Background = GetFluentColor("SolidBackgroundFillColorTertiary"),
                        Stroke = GetFluentColor("ControlElevationBorder")
                    };
                    Annotations.Add(_resultAnnotation);
                }

                _resultAnnotation.Text = $"Max: x={bestX:0.######} y={bestY:0.######}";
                _resultAnnotation.TextPosition = new DataPoint(bestX, bestY);


                InvalidatePlot(false); // 刷新图表
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(object sender, OxyMouseEventArgs e)
        {
            base.OnMouseMove(sender, e);
            // 如果处于框选模式并且鼠标左键按下，则更新选择矩形
            if (_selectionEnabled && _selectRect != null && !double.IsNaN(_selectStart))
            {
                var cur = InverseTransform(e.Position);
                _selectRect.MinimumX = Math.Min(_selectStart, cur.X);
                _selectRect.MaximumX = Math.Max(_selectStart, cur.X);

                InvalidatePlot(false); // 刷新界面
                e.Handled = true; // 防止事件继续传播
            }
        }

        protected override void OnMouseDown(object sender, OxyMouseDownEventArgs e)
        {
            base.OnMouseDown(sender, e);

            var sp = e.Position;

            // 检测图标按钮点击
            foreach (var btn in new[] {_btnCopy, _btnSelect, _btnLogToggle })
            {
                if (btn.ScreenRectangle.Contains(sp))
                {
                    btn.ClickAction?.Invoke();
                    e.Handled = true;
                    return;
                }
            }

            // 如果处于框选模式，开始选择
            if (_selectionEnabled && e.ChangedButton == OxyMouseButton.Left)
            {
                // 记录选择开始位置
                _selectStart = InverseTransform(e.Position).X;
                _selectRect.MinimumX = _selectStart;
                _selectRect.MaximumX = _selectStart;
                InvalidatePlot(false); // 刷新界面
                e.Handled = true; // 防止事件继续传播
            }
        }

        private void ToggleCopy()
        {
            var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            CopyAlignedSeriesDataToClipboard(manWin);
        }

        private void ToggleSelection()
        {
            if (!_selectionEnabled)
            {
                // 启动框选
                _selectionEnabled = true;
                _selectRect = new RectangleAnnotation
                {
                    Fill = OxyColor.FromAColor(120, OxyColors.SkyBlue),
                    Layer = AnnotationLayer.AboveSeries,
                    MinimumX = 0,
                    MaximumX = 0,
                };
                Annotations.Add(_selectRect);

                // 切换按钮状态
                _btnSelect.IsToggled = true;

            }
            else
            {
                // 取消框选
                _selectionEnabled = false;
                if (_selectRect != null)
                    Annotations.Remove(_selectRect);
                if (_resultAnnotation != null)
                    Annotations.Remove(_resultAnnotation);
                _selectRect = null;
                _resultAnnotation = null;

                // 切换按钮状态
                _btnSelect.IsToggled = false;

            }

            InvalidatePlot(false); // 刷新图表以显示更新后的按钮
        }

        private void ToggleLogScale()
        {
            var updatedAxes = new List<Axis>();
            foreach (var axis in Axes.ToList())
            {
                if (axis.Position == AxisPosition.Left || axis.Position == AxisPosition.Right)
                {
                    if (axis is LinearAxis lin)
                    {
                        var log = new LogarithmicAxis
                        {
                            Position = lin.Position,
                            Title = lin.Title,
                            Key = lin.Key,
                            Minimum = double.NaN,
                            Maximum = double.NaN
                        };
                        updatedAxes.Add(log);
                    }
                    else if (axis is LogarithmicAxis logAx)
                    {
                        var lin2 = new LinearAxis
                        {
                            Position = logAx.Position,
                            Title = logAx.Title,
                            Key = logAx.Key,
                            Minimum = double.NaN,
                            Maximum = double.NaN
                        };
                        updatedAxes.Add(lin2);
                    }
                    else
                    {
                        updatedAxes.Add(axis);
                    }
                }

            }

            Axes.Clear();
            foreach (var ax in updatedAxes)
                Axes.Add(ax);

            _isLogScale = !_isLogScale;
            _btnLogToggle.IsToggled = _isLogScale;

            this.ResetAllAxes();
            InvalidatePlot(false);
        }



        protected override void RenderOverride(IRenderContext rc, OxyRect rect)
        {
            var area = this.PlotArea;
            var right = area.Right - 150;
            _btnCopy.X = right - _btnCopy.Width - 10;
            _btnCopy.Y = area.Top + _btnCopy.Height / 2 + 10;
            _btnSelect.X = right - _btnSelect.Width * 2 - 20;
            _btnSelect.Y = area.Top + _btnSelect.Height / 2 + 10;
            //_btnLogToggle.X = right - _btnLogToggle.Width * 3 - 30;
            //_btnLogToggle.Y = area.Top + _btnLogToggle.Height / 2 + 10;
            base.RenderOverride(rc, rect);
        }

        private DataPoint InverseTransform(ScreenPoint pt)
        {
            var xAxis = this.DefaultXAxis ?? this.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
            var yAxis = this.DefaultYAxis ?? this.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);

            if (xAxis == null || yAxis == null)
                return new DataPoint();

            double x = xAxis.InverseTransform(pt.X);
            double y = yAxis.InverseTransform(pt.Y);
            return new DataPoint(x, y);
        }


        public void ApplyTheme()
        {

            this.Background = GetFluentColor("SolidBackgroundFillColorBase");
            this.PlotAreaBorderColor = GetFluentColor("ControlElevationBorder");
            this.PlotAreaBackground = GetFluentColor("SolidBackgroundFillColorQuarternary");
            this.TextColor = GetFluentColor("TextFillColorPrimary");

            // 坐标轴样式
            foreach (var axis in this.Axes)
            {
                axis.AxislineColor = GetFluentColor("SystemAccentColor");
                axis.TextColor = this.TextColor;
                axis.TicklineColor = GetFluentColor("ControlStrokeColorSecondary");
                axis.MajorGridlineColor = GetFluentColor("ControlStrokeColorDefault")
                    .WithAlpha(0.4);
            }

            // 数据系列颜色
            var accentColor = GetFluentColor("SystemAccentColor");
            int index = 0;
            foreach (var series in this.Series)
            {
                if (series is OxyPlot.Series.LineSeries line)
                {
                    line.Color = GenerateSeriesColor(accentColor, index++);
                }
            }

            foreach (var legend in this.Legends)
            {
                legend.TextColor = this.TextColor;
                legend.LegendTitleColor = this.TitleColor;
                legend.LegendBackground = GetFluentColor("SolidBackgroundFillColorTertiary");
                legend.LegendBorder = GetFluentColor("ControlElevationBorder");
            }

            foreach (var ann in this.Annotations)
            {
                ann.TextColor = this.TextColor;
                if (ann is TextAnnotation txAnn)
                {
                    txAnn.TextColor = this.TextColor;
                    txAnn.Background = GetFluentColor("SolidBackgroundFillColorTertiary");
                    txAnn.Stroke = GetFluentColor("ControlElevationBorder");
                }


            }

            _btnCopy.TextColor = this.TextColor;
            _btnCopy.BorderColor = this.PlotAreaBorderColor;
            _btnCopy.FillColor = GetFluentColor("SolidBackgroundFillColorTertiary");

            _btnSelect.TextColor = this.TextColor;
            _btnSelect.BorderColor = this.PlotAreaBorderColor;
            _btnSelect.FillColor = GetFluentColor("SolidBackgroundFillColorTertiary");

            _btnLogToggle.TextColor = this.TextColor;
            _btnLogToggle.BorderColor = this.PlotAreaBorderColor;
            _btnLogToggle.FillColor = GetFluentColor("SolidBackgroundFillColorTertiary");

            InvalidatePlot(true);

        }


        private OxyColor GetFluentColor(string key)
        {
            var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            var theme = Application.Current.ActualThemeVariant;
            if (theme == ThemeVariant.Dark)
            {
                var color = manWin.TryFindResource(key,
                    ThemeVariant.Dark, out var value)
                    ? (Color2)(Color)value
                    : new Color2(32, 32, 32);
                return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }
            else if (theme == ThemeVariant.Light)
            {
                var color = manWin.TryFindResource(key,
                    ThemeVariant.Light, out var value)
                    ? (Color2)(Color)value
                    : new Color2(32, 32, 32);
                return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
            }

            return OxyColor.FromArgb(0xFF, 32, 32, 32);
        }

        private OxyColor GenerateSeriesColor(OxyColor baseColor, int index)
        {
            switch (index % 3)
            {
                case 0: return baseColor;
                case 1: return baseColor.ChangeHue(30);
                case 2: return baseColor.ChangeHue(40);

            }

            return baseColor;
        }



        public void CopyAlignedSeriesDataToClipboard(TopLevel topLevel)
        {
            var lineSeriesList = this.Series.OfType<LineSeries>().ToList()
                .Select(l => l.ItemsSource as IEnumerable<DataPoint>).ToList();
            var headers = this.Series.OfType<LineSeries>().ToList().Select(l => l.Title).ToList();
            // 计算最大点数
            int maxCount = lineSeriesList.Max(s=>s.Count());

            var sb = new StringBuilder();


            // 遍历每个点的索引
            for (int i = 0; i < maxCount; i++)
            {
                for (int j = 0; j < lineSeriesList.Count; j++)
                {
                    var points = lineSeriesList[j];
                    if (i < points.Count())
                    {
                        var pt = points.ElementAt(i);
                        sb.Append($"{pt.X}\t{pt.Y}");
                    }
                    else
                    {
                        sb.Append("\t"); // 空值
                    }

                    if (j < lineSeriesList.Count - 1)
                        sb.Append("\t");
                }

                sb.AppendLine();
            }

            // 复制到剪贴板
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null)
            {
                clipboard.SetTextAsync(sb.ToString());
            }
        }

    }
}
