


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
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SCSA.Utils;

namespace QuickMA.Modules.Plot
{
    public class CuPlotModel : PlotModel, IPlotModel, INotifyPropertyChanged
    {
        private InteractionMode _selectedMode = InteractionMode.None;
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


        private PlotController _defaultController;
        private PlotController _zoomController;
        private PlotController _rangeSelectController;
        private PlotController _panController;
        private IController _controller;
        private string _title;
        private TextAnnotation _titleAnnotation;
        private List<RangeResult> _rangeResults;
        private string _xUint;
        private string _yUnit;
        private string _xTitle;
        private string _yTitle;
        private bool _showToolBar;
        private string _subTitle;
        private TextAnnotation _subTitleAnnotation;


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


        public CuPlotModel(bool showToolBar = true)
        {
            Application.Current.ActualThemeVariantChanged += (s, e) => ApplyTheme();
            ApplyTheme();
            PlotMargins = new OxyThickness(40, 20, 0, double.NaN);

            this.IsLegendVisible = true;

            var legend = new Legend()
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
            };
            this.Legends.Add(legend);

            _showToolBar = showToolBar;

            DefaultFont = "Microsoft YaHei";
            SetupControllers();
            SetupButtons();
            SetupTitle();


            this.PropertyChanged += CuPlotModel_PropertyChanged;

        }



        private void CuPlotModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XUint))
            {
                foreach (var axise in Axes.Where(ax=>ax.Position== AxisPosition.Bottom)
                             .ToList())
                {
                    axise.Unit = XUint;
                }
            }
            else if (e.PropertyName == nameof(Title))
                TitleAnnotation.Text = Title;
            else if (e.PropertyName == nameof(SubTitle))
                SubTitleAnnotation.Text = SubTitle;
        }

        private void SetupTitle()
        {
            TitleAnnotation = new TextAnnotation()
            {
                X = new PlotLength(1, PlotLengthUnit.RelativeToViewport),
                Y = new PlotLength(0, PlotLengthUnit.RelativeToViewport),
                //OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                VerticalAlignment = OxyPlot.VerticalAlignment.Top,
                FontWeight = OxyPlot.FontWeights.Bold,
                Text = "",
                Tag = "SystemAnnotation"
            };
            this.Annotations.Add(TitleAnnotation);

            SubTitleAnnotation = new TextAnnotation()
            {
                X = new PlotLength(1, PlotLengthUnit.RelativeToViewport),
                Y = new PlotLength(0, PlotLengthUnit.RelativeToViewport),
                OffsetX = new PlotLength(-130, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
                VerticalAlignment = OxyPlot.VerticalAlignment.Top,
                Text = "",
                Tag = "SystemAnnotation"
            };

            this.Annotations.Add(SubTitleAnnotation);
        }
        private void SetupButtons()
        {
            if (!_showToolBar)
                return;
            //var uri = new Uri("pack://application:,,,/QuickMA;component/Resources/Fonts/materialdesignicons-webfont.ttf", UriKind.Absolute);
            var container = new ButtonAnnotationContainer()
            {
                OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits),
                OffsetY = new PlotLength(-5, PlotLengthUnit.ScreenUnits),
                Horizontal = true,
                Spacing = 3,
            };
            var btnPan = new ToggleButtonAnnotation()
            {
                Text = "P",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                GroupName = "ToolBar",
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE616),
                Tag = "SystemAnnotation",
                TextColor = TextColor,
            };
            btnPan.Toggled += (s, e) => SelectedMode = e.IsChecked ? InteractionMode.Pan : InteractionMode.None;

            var btnZoom = new ToggleButtonAnnotation()
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
            var btnRange = new ToggleButtonAnnotation()
            {
                Text = "S",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                GroupName = "ToolBar",
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE7FC),
                Tag = "SystemAnnotation",
                TextColor = TextColor,
            };
            btnRange.Toggled += (s, e) =>
                SelectedMode = e.IsChecked ? InteractionMode.RangeSelect : InteractionMode.None;

            var btnLock = new ToggleButtonAnnotation()
            {
                Text = "L",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE676),
                Tag = "SystemAnnotation",
                TextColor = TextColor,
            };


            var originalMin = new Dictionary<Axis, double?>();

            btnLock.Toggled += (s, e) =>
            {
                if (e.IsChecked)
                {
                    // 存储并归零
                    originalMin.Clear();
                    foreach (var ax in Axes)
                    {
                        originalMin[ax] = ax.AbsoluteMinimum;
                        ax.AbsoluteMinimum = 0;
                    }

                }
                else
                {
                    // 恢复原值
                    foreach (var kv in originalMin)
                        kv.Key.AbsoluteMinimum = kv.Value ?? double.NaN;

                }
                this.ResetAllAxes();
                InvalidatePlot(false);
            };

            var btnLog = new ToggleButtonAnnotation()
            {
                Text = "LL",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE690),
                Tag = "SystemAnnotation",
                TextColor = TextColor,
            };
            btnLog.Toggled += (s, e) =>
            {
                var updatedAxes = new List<Axis>();
                var removeAxes = new List<Axis>();
                if (e.IsChecked)
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
                        }
                        removeAxes.Add(axis);
                    }
                }

                foreach (var ax in removeAxes)
                    Axes.Remove(ax);

                foreach (var ax in updatedAxes)
                    Axes.Add(ax);

                this.ResetAllAxes();
                InvalidatePlot(false);
            };

            var btnCopy = new ButtonAnnotation()
            {
                Text = "C",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE706),
                Tag = "SystemAnnotation",
                TextColor = TextColor,
            };
            btnCopy.Pressed += (s, e) => CopyAlignedSeriesDataToClipboard();
            var btnScreenShot = new ButtonAnnotation()
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
                var parentUc = ((OxyPlot.Avalonia.PlotView)this.PlotView).FindAncestorOfType<UserControl>();
                var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
              
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                   await ScreenshotHelper.CaptureAndSaveControlAsync(parentUc, manWin);
                });
     

            };
            var btnReset = new ButtonAnnotation()
            {
                Text = "R",
                Height = new PlotLength(20, PlotLengthUnit.ScreenUnits),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                IconFontSize = 24,
                IconText = char.ConvertFromUtf32(0xE79B),
                Tag = "SystemAnnotation",
                TextColor = TextColor,

            };
            btnReset.Pressed += (s, e) =>
            {
                var btns = this.Annotations.OfType<ToggleButtonAnnotation>().ToList();
                foreach (var ann in btns)
                {
                    ann.SetChecked(false);
                }
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
 
            this.Background = GetFluentColor("SolidBackgroundFillColorBase");
            this.PlotAreaBorderColor = GetFluentColor("ControlElevationBorder");
            this.PlotAreaBackground = GetFluentColor("SolidBackgroundFillColorQuarternary");
            this.TextColor = GetFluentColor("TextFillColorPrimary");



            // 数据系列颜色
            var accentColor = GetFluentColor("SystemAccentColor");

            foreach (var legend in this.Legends)
            {
                legend.LegendBackground = Background;
                legend.LegendBorder = PlotAreaBorderColor;
                legend.TextColor = TextColor;
            }
            // 坐标轴样式
            foreach (var axis in this.Axes)
            {
                axis.AxislineColor = GetFluentColor("SystemAccentColor");
                axis.TextColor = this.TextColor;
                axis.TicklineColor = GetFluentColor("ControlStrokeColorSecondary");
                axis.MajorGridlineColor = GetFluentColor("ControlStrokeColorDefault")
                    .WithAlpha(0.4);
            }

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

            TextAnnotation.IconCache.Clear();

            this.PlotAreaBackground = OxyColors.Transparent;
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
            var removeList = this.Annotations
                .Except(this.Annotations.Where(ann => ann.Tag != null && ann.Tag.ToString() == "SystemAnnotation"))
                .ToList();
            foreach (var annotation in removeList)
            {
                this.Annotations.Remove(annotation);
            }
        }

        public void CopyAlignedSeriesDataToClipboard()
        {
            var manWin = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            var lineSeriesList = this.Series.OfType<LineSeries>().ToList()
                .Select(l => l.ItemsSource as IEnumerable<DataPoint>).ToList();
         
            if(lineSeriesList.Count==0)
                return;
            // 计算最大点数
            int maxCount = lineSeriesList.Max(s => s.Count());

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
            var clipboard = manWin?.Clipboard;
            if (clipboard != null)
            {
                clipboard.SetTextAsync(sb.ToString());
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

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
    }
}
