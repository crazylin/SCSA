using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Media;
using OxyPlot;
using OxyPlot.Avalonia;
using OxyPlot.Axes;
using SCSA.Utils;
using Axis = OxyPlot.Axes.Axis;
using Legend = OxyPlot.Legends.Legend;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using LogarithmicAxis = OxyPlot.Axes.LogarithmicAxis;
using RectangleAnnotation = OxyPlot.Annotations.RectangleAnnotation;

namespace SCSA.Plot;

public class CuPlotModel : PlotModel, IPlotModel, INotifyPropertyChanged
{
    private IController _controller;


    private PlotController _defaultController;
    private PlotController _panController;
    private List<RangeResult> _rangeResults;
    private PlotController _rangeSelectController;
    private InteractionMode _selectedMode = InteractionMode.None;
    private bool _showToolBar;
    private string _subTitle;
    private TextAnnotation _subTitleAnnotation;
    private string _title;
    private TextAnnotation _titleAnnotation;
    private string _xTitle;
    private string _xUint;
    private string _yTitle;
    private string _yUnit;
    private PlotController _zoomController;
    private readonly Dictionary<Axis, double?> _originalMin = new();


    public CuPlotModel(bool showToolBar = true)
    {
        Application.Current.ActualThemeVariantChanged += (s, e) => ApplyTheme();
        ApplyTheme();
        PlotMargins = new OxyThickness(40, 0, 0, double.NaN);
     
        IsLegendVisible = true;

        var legend = new Legend
        {
            LegendBorder = OxyColors.Black,
            LegendBackground = OxyColor.FromAColor(200, OxyColors.White)
        };
        Legends.Add(legend);

        _showToolBar = showToolBar;

        DefaultFont = "Microsoft YaHei";
        SetupControllers();
        //SetupButtons();
        //SetupTitle();


        PropertyChanged += CuPlotModel_PropertyChanged;
    }

    public InteractionMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetField(ref _selectedMode, value))
            {
                // 切换 Controller
                switch (_selectedMode)
                {
                    case InteractionMode.Zoom:
                        Controller = _zoomController; break;
                    case InteractionMode.RangeSelect:
                        Controller = _rangeSelectController; break;
                    case InteractionMode.Pan:
                        Controller = _panController; break;
                    default:

                        Controller = _defaultController; break;
                }

                if (_selectedMode != InteractionMode.RangeSelect)
                {
                    var rect = Annotations.OfType<RectangleAnnotation>()
                        .FirstOrDefault(a => a.Tag?.ToString() == "RangeSelect");
                    if (rect != null)
                    {
                        Annotations.Remove(rect);
                        RangeResults = new List<RangeResult>();
                    }
                }

                InvalidatePlot(true);
            }
        }
    }


    public new string Title
    {
        set => SetField(ref _title, value);
        get => _title;
    }

    public List<RangeResult> RangeResults
    {
        set => SetField(ref _rangeResults, value);
        get => _rangeResults;
    }

    public TextAnnotation TitleAnnotation
    {
        set => SetField(ref _titleAnnotation, value);
        get => _titleAnnotation;
    }

    public TextAnnotation SubTitleAnnotation
    {
        set => SetField(ref _subTitleAnnotation, value);
        get => _subTitleAnnotation;
    }

    public string XUint
    {
        set => SetField(ref _xUint, value);
        get => _xUint;
    }

    public string YUnit
    {
        set => SetField(ref _yUnit, value);
        get => _yUnit;
    }

    public string XTitle
    {
        set => SetField(ref _xTitle, value);
        get => _xTitle;
    }

    public string YTitle
    {
        set => SetField(ref _yTitle, value);
        get => _yTitle;
    }

    public IController Controller
    {
        set => SetField(ref _controller, value);
        get => _controller;
    }

    public bool ShowToolBar
    {
        set => SetField(ref _showToolBar, value);
        get => _showToolBar;
    }


    public string SubTitle
    {
        set => SetField(ref _subTitle, value);
        get => _subTitle;
    }


    public event PropertyChangedEventHandler? PropertyChanged;


    private void CuPlotModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(XUint))
            foreach (var axise in Axes.Where(ax => ax.Position == AxisPosition.Bottom)
                         .ToList())
                axise.Unit = XUint;
        //else if (e.PropertyName == nameof(Title))
        //    TitleAnnotation.Text = Title;
        //else if (e.PropertyName == nameof(SubTitle))
        //    SubTitleAnnotation.Text = SubTitle;
    }

    private void SetupTitle()
    {
        TitleAnnotation = new TextAnnotation
        {
            X = new PlotLength(1, PlotLengthUnit.RelativeToViewport),
            Y = new PlotLength(0, PlotLengthUnit.RelativeToViewport),
            //OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            FontWeight = FontWeights.Bold,
            Text = "",
            Tag = "SystemAnnotation"
        };
        Annotations.Add(TitleAnnotation);

        SubTitleAnnotation = new TextAnnotation
        {
            X = new PlotLength(1, PlotLengthUnit.RelativeToViewport),
            Y = new PlotLength(0, PlotLengthUnit.RelativeToViewport),
            OffsetX = new PlotLength(-130, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Text = "",
            Tag = "SystemAnnotation"
        };

        Annotations.Add(SubTitleAnnotation);
    }




    private void SetupButtons()
    {


        if (!_showToolBar)
            return;
        //var uri = new Uri("pack://application:,,,/QuickMA;component/Resources/Fonts/materialdesignicons-webfont.ttf", UriKind.Absolute);
        var container = new ButtonAnnotationContainer
        {
            OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits),
            OffsetY = new PlotLength(-5, PlotLengthUnit.ScreenUnits),
            Horizontal = true,
            Spacing = 3
        };
        var btnPan = new ToggleButtonAnnotation
        {
            Text = "P",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            GroupName = "ToolBar",
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE616),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };
        btnPan.Toggled += (s, e) => SelectedMode = e.IsChecked ? InteractionMode.Pan : InteractionMode.None;

        var btnZoom = new ToggleButtonAnnotation
        {
            Text = "Z",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            GroupName = "ToolBar",
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE7FF),
            Tag = "SystemAnnotation"
        };
        btnZoom.Toggled += (s, e) => SelectedMode = e.IsChecked ? InteractionMode.Zoom : InteractionMode.None;
        var btnRange = new ToggleButtonAnnotation
        {
            Text = "S",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            GroupName = "ToolBar",
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE7FC),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };
        btnRange.Toggled += (s, e) =>
            SelectedMode = e.IsChecked ? InteractionMode.RangeSelect : InteractionMode.None;

        var btnLock = new ToggleButtonAnnotation
        {
            Text = "L",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE676),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };


        btnLock.Toggled += (s, e) =>
        {
            if (e.IsChecked)
            {
                // 存储并归零
                _originalMin.Clear();
                foreach (var ax in Axes)
                {
                    _originalMin[ax] = ax.AbsoluteMinimum;
                    ax.AbsoluteMinimum = 0;
                }
            }
            else
            {
                // 恢复原值
                foreach (var kv in _originalMin)
                    kv.Key.AbsoluteMinimum = kv.Value ?? double.NaN;
            }

            ResetAllAxes();
            InvalidatePlot(false);
        };

        var btnLog = new ToggleButtonAnnotation
        {
            Text = "LL",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE690),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };
        btnLog.Toggled += (s, e) =>
        {
            var updatedAxes = new List<Axis>();
            var removeAxes = new List<Axis>();
            if (e.IsChecked)
            {
                foreach (var axis in Axes.ToList())
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
                        else
                        {
                            updatedAxes.Add(axis);
                        }

                        removeAxes.Add(axis);
                    }
            }
            else
            {
                foreach (var axis in Axes.ToList())
                {
                    if (axis.Position == AxisPosition.Left || axis.Position == AxisPosition.Right)
                    {
                        if (axis is LogarithmicAxis logAx)
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

                    removeAxes.Add(axis);
                }
            }

            foreach (var ax in removeAxes)
                Axes.Remove(ax);

            foreach (var ax in updatedAxes)
                Axes.Add(ax);

            ResetAllAxes();
            InvalidatePlot(false);
        };

        var btnCopy = new ButtonAnnotation
        {
            Text = "C",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE706),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };
        btnCopy.Pressed += (s, e) => CopyAlignedSeriesDataToClipboard();
        var btnScreenShot = new ButtonAnnotation
        {
            Text = "S",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE642),
            Tag = "SystemAnnotation"
        };

        btnScreenShot.Pressed += (s, e) =>
        {
            var parentUc = ((PlotView)PlotView).FindAncestorOfType<UserControl>();
            var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ScreenshotHelper.CaptureAndSaveControlAsync(parentUc, manWin);
            });
        };
        var btnReset = new ButtonAnnotation
        {
            Text = "R",
            Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IconFontSize = 24,
            IconText = char.ConvertFromUtf32(0xE79B),
            Tag = "SystemAnnotation",
            TextColor = TextColor
        };
        btnReset.Pressed += (s, e) =>
        {
            var btns = Annotations.OfType<ToggleButtonAnnotation>().ToList();
            foreach (var ann in btns) ann.SetChecked(false);
            ResetAllAxes();

            SelectedMode = InteractionMode.None;

            InvalidatePlot(false);
        };


        container.Add(btnPan);
        container.Add(btnZoom);
        container.Add(btnRange);
        container.Add(btnLog);
        container.Add(btnLock);
        container.Add(btnCopy);
        container.Add(btnScreenShot);
        container.Add(btnReset);


        container.Apply(this);
    }

    public void ApplyTheme()
    {
        Background = OxyColors.Transparent;// GetFluentColor("SolidBackgroundFillColorBase");
        PlotAreaBackground = OxyColors.Transparent; //GetFluentColor("SolidBackgroundFillColorBase");
        TextColor = GetFluentColor("TextFillColorPrimary");
        PlotAreaBorderColor = TextColor; //GetFluentColor("ControlElevationBorder");

        // 数据系列颜色
        var accentColor = GetFluentColor("SystemAccentColor");

        foreach (var legend in Legends)
        {
            legend.LegendBackground = Background;
            legend.LegendBorder = PlotAreaBorderColor;
            legend.TextColor = TextColor;
        }

        // 坐标轴样式
        foreach (var axis in Axes)
        {
            axis.AxislineColor = GetFluentColor("SystemAccentColor");
            axis.TextColor = TextColor;
            axis.TicklineColor = TextColor.WithAlpha(0.8); //GetFluentColor("ControlStrokeColorSecondary");
            axis.MajorGridlineColor = GetFluentColor("ControlStrokeColorDefault")
                .WithAlpha(0.4);
        }

        var index = 0;
        foreach (var series in Series)
            if (series is LineSeries line)
                line.Color = GenerateSeriesColor(accentColor, index++);

        foreach (var legend in Legends)
        {
            legend.TextColor = TextColor;
            legend.LegendTitleColor = TitleColor;
            legend.LegendBackground = GetFluentColor("SolidBackgroundFillColorTertiary");
            legend.LegendBorder = TextColor.WithAlpha(0.8);  //GetFluentColor("ControlElevationBorder");
        }

        foreach (var ann in Annotations)
        {
            ann.TextColor = TextColor;
            if (ann is TextAnnotation txAnn)
            {
                txAnn.TextColor = TextColor;
                txAnn.Background = GetFluentColor("SolidBackgroundFillColorTertiary");
                txAnn.Stroke = GetFluentColor("ControlElevationBorder");
            }
            //else if (ann is RectangleAnnotation rAnn)
            //{
            //    rAnn.TextColor = TextColor;
            //    rAnn.Stroke = GetFluentColor("ControlElevationBorder");
            //}
        }

        TextAnnotation.IconCache.Clear();

        PlotAreaBackground = OxyColors.Transparent;
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

        if (theme == ThemeVariant.Light)
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

    private void SetupControllers()
    {
        // Default controller with pan functionality
        _defaultController = new PlotController();

        // Zoom controller
        _zoomController = new PlotController();
        _zoomController.BindMouseDown(OxyMouseButton.Left, new DelegatePlotCommand<OxyMouseDownEventArgs>(
            (view, controller, args) =>
            {
                controller.AddMouseManipulator(view, new XZoomRectangleManipulator(view), args);
            }));

        // Pan controller
        _panController = new PlotController();
        _panController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);

        // Range selection controller
        _rangeSelectController = new PlotController();
        _rangeSelectController.BindMouseDown(OxyMouseButton.Left, new DelegatePlotCommand<OxyMouseDownEventArgs>(
            (view, controller, args) =>
            {
                controller.AddMouseManipulator(view, new RangeSelectionManipulator(view), args);
            }));


        Controller = _defaultController;
    }

    public void ClearAnnotations()
    {
        var removeList = Annotations
            .Except(Annotations.Where(ann => ann.Tag != null && ann.Tag.ToString() == "SystemAnnotation"))
            .ToList();
        foreach (var annotation in removeList) Annotations.Remove(annotation);
    }

    public void CopyAlignedSeriesDataToClipboard()
    {
        var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
        var lineSeriesList = Series.OfType<LineSeries>()
            .Select(ls => ls.ItemsSource as IEnumerable<DataPoint> ?? ls.Points)
            .Where(points => points != null)
            .ToList();

        if (lineSeriesList.Count == 0)
            return;
        // 计算最大点数
        var maxCount = lineSeriesList.Max(s => s.Count());

        var sb = new StringBuilder();


        // 遍历每个点的索引
        for (var i = 0; i < maxCount; i++)
        {
            for (var j = 0; j < lineSeriesList.Count; j++)
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
        var clipboard = manWin?.Clipboard;
        if (clipboard != null) clipboard.SetTextAsync(sb.ToString());
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #region External Toolbar Helpers

    /// <summary>
    /// 切换 X 轴锁定（AbsoluteMinimum = 0）。
    /// </summary>
    public void ToggleLock(bool isChecked)
    {
        if (isChecked)
        {
            _originalMin.Clear();
            foreach (var ax in Axes)
            {
                _originalMin[ax] = ax.AbsoluteMinimum;
                ax.AbsoluteMinimum = 0;
            }
        }
        else
        {
            foreach (var kv in _originalMin)
                kv.Key.AbsoluteMinimum = kv.Value ?? double.NaN;
        }

        ResetAllAxes();
        InvalidatePlot(false);
    }

    /// <summary>
    /// 切换 Y 轴线性 / 对数。
    /// </summary>
    public void ToggleLog(bool isChecked)
    {
        var updatedAxes = new List<Axis>();
        var removeAxes = new List<Axis>();

        if (isChecked)
        {
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
                    else
                    {
                        updatedAxes.Add(axis);
                    }
                    removeAxes.Add(axis);
                }
            }
        }
        else
        {
            foreach (var axis in Axes.ToList())
            {
                if (axis.Position == AxisPosition.Left || axis.Position == AxisPosition.Right)
                {
                    if (axis is LogarithmicAxis logAx)
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
                    removeAxes.Add(axis);
                }
            }
        }

        foreach (var ax in removeAxes)
            Axes.Remove(ax);

        foreach (var ax in updatedAxes)
            Axes.Add(ax);

        ResetAllAxes();
        InvalidatePlot(false);
    }

    /// <summary>
    /// 复位坐标系并清除交互模式。
    /// </summary>
    public void ResetPlot()
    {
        SelectedMode = InteractionMode.None;
        ResetAllAxes();
        InvalidatePlot(false);
    }

    #endregion
}