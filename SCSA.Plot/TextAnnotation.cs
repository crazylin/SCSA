using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Avalonia;

namespace QuickMA.Modules.Plot
{
    public class TextAnnotation: TransposableAnnotation,INotifyPropertyChanged
    {
        /// <summary>Occurs when the toggle state changes.</summary>
        public event EventHandler<HitTestArguments> Pressed;
        /// <summary>
        /// Raises the Toggled event.
        /// </summary>
        protected virtual void OnPressed(HitTestArguments e)
        {
            Pressed?.Invoke(this, e);
        }
        protected OxyRect actualBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleButtonAnnotation" /> class.
        /// </summary>
        public TextAnnotation()
        {
            X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea);
            Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea);
            OffsetX = new PlotLength(0.0, PlotLengthUnit.ScreenUnits);
            OffsetY = new PlotLength(0.0, PlotLengthUnit.ScreenUnits);
            Width = new PlotLength(25, PlotLengthUnit.ScreenUnits);
            Height = new PlotLength(25, PlotLengthUnit.ScreenUnits);
            Stroke = OxyColors.Black;
            TextColor = OxyColors.Black;
            BorderThickness = 0.5;
            CornerRadius = 4;
            Opacity = 1.0;
            FontSize = 12;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Middle;
            Text = "Text";
            FontWeight = FontWeights.Normal;

            IconText = null;
            IconFontFamily = null;
            IconFontSize = 16;
            IconColor = OxyColors.Black;
        }

        #region Properties

        /// <summary>Gets or sets the checked state color.</summary>
        public OxyColor Background { get; set; }



        /// <summary>Gets or sets the border color.</summary>
        public OxyColor Stroke { get; set; }

        /// <summary>Gets or sets the text color.</summary>
        public OxyColor TextColor
        {
            set
            {
                if (value.Equals(_textColor)) return;
                _textColor = value;
                IconColor = value;
                OnPropertyChanged();
            }
            get => _textColor;
        }

        /// <summary>Gets or sets the border thickness.</summary>
        public double BorderThickness { get; set; }

        /// <summary>Gets or sets the corner radius for rounded corners.</summary>
        public double CornerRadius { get; set; }

        /// <summary>Gets or sets the horizontal alignment.</summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>Gets or sets the vertical alignment.</summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>Gets or sets the X position.</summary>
        public PlotLength X { get; set; }

        /// <summary>Gets or sets the Y position.</summary>
        public PlotLength Y { get; set; }

        /// <summary>Gets or sets the X offset.</summary>
        public PlotLength OffsetX { get; set; }

        /// <summary>Gets or sets the Y offset.</summary>
        public PlotLength OffsetY { get; set; }

        /// <summary>Gets or sets the width.</summary>
        public PlotLength Width { get; set; }

        /// <summary>Gets or sets the height.</summary>
        public PlotLength Height { get; set; }

        /// <summary>Gets or sets the opacity (0-1).</summary>
        public double Opacity { get; set; }

        /// <summary>Gets or sets the font size for the button text.</summary>
        public double FontSize { get; set; }

        /// <summary>Gets or sets the font weight for the button text.</summary>
        public double FontWeight { get; set; }

        /// <summary>Gets or sets the button text.</summary>
        /// <remarks>
        /// If CheckedText or UncheckedText is set, they will override this text
        /// based on the button state.
        /// </remarks>
        public string Text { get; set; }


        /// <summary>Icon glyph to display (e.g. from FontAwesome).</summary>
        public string IconText { get; set; }

        /// <summary>Font family for the icon (e.g. "FontAwesome").</summary>
        public string IconFontFamily { get; set; }

        /// <summary>Size of the icon font.</summary>
        public double IconFontSize { get; set; }

        /// <summary>Color of the icon.</summary>
        public OxyColor IconColor { get; set; }
        #endregion



        /// <summary>
        /// Tests if the button is hit by the specified point.
        /// </summary>
        protected override HitTestResult HitTestOverride(HitTestArguments args)
        {
            if (actualBounds.Contains(args.Point))
            {
                OnPressed(args);
                return new HitTestResult(this, args.Point);
            }
            return null;
        }



        #region Position Calculation Methods

        protected ScreenPoint GetPoint(PlotLength x, PlotLength y, PlotModel model)
        {
            double x1 = double.NaN;
            double y1 = double.NaN;

            if (x.Unit == PlotLengthUnit.Data || y.Unit == PlotLengthUnit.Data)
            {
                ScreenPoint screenPoint = Transform(
                 new DataPoint(x.Unit == PlotLengthUnit.Data ? x.Value : double.NaN,
                     y.Unit == PlotLengthUnit.Data ? y.Value : double.NaN));
                x1 = screenPoint.X;
                y1 = screenPoint.Y;
            }

            OxyRect plotArea;
            switch (x.Unit)
            {
                case PlotLengthUnit.Data:
                    switch (y.Unit)
                    {
                        case PlotLengthUnit.Data:
                            return new ScreenPoint(x1, y1);
                        case PlotLengthUnit.ScreenUnits:
                            y1 = y.Value;
                            break;
                        case PlotLengthUnit.RelativeToViewport:
                            y1 = model.Height * y.Value;
                            break;
                        case PlotLengthUnit.RelativeToPlotArea:
                            plotArea = model.PlotArea;
                            y1 = plotArea.Top + (plotArea.Height * y.Value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    return new ScreenPoint(x1, y1);

                case PlotLengthUnit.ScreenUnits:
                    x1 = x.Value;
                    break;

                case PlotLengthUnit.RelativeToViewport:
                    x1 = model.Width * x.Value;
                    break;

                case PlotLengthUnit.RelativeToPlotArea:
                    plotArea = model.PlotArea;
                    x1 = plotArea.Left + (plotArea.Width * x.Value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (y.Unit)
            {
                case PlotLengthUnit.Data:
                    return new ScreenPoint(x1, y1);
                case PlotLengthUnit.ScreenUnits:
                    return new ScreenPoint(x1, y.Value);
                case PlotLengthUnit.RelativeToViewport:
                    return new ScreenPoint(x1, model.Height * y.Value);
                case PlotLengthUnit.RelativeToPlotArea:
                    plotArea = model.PlotArea;
                    return new ScreenPoint(x1, plotArea.Top + (plotArea.Height * y.Value));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected ScreenVector GetVector(PlotLength x, PlotLength y, PlotModel model)
        {
            double x1 = double.NaN;
            double y1 = double.NaN;

            if (x.Unit == PlotLengthUnit.Data || y.Unit == PlotLengthUnit.Data)
            {
                ScreenVector screenVector = Transform(
                    new DataPoint(x.Unit == PlotLengthUnit.Data ? x.Value : double.NaN,
                        y.Unit == PlotLengthUnit.Data ? y.Value : double.NaN)) - Transform(new DataPoint(0.0, 0.0));
                x1 = screenVector.X;
                y1 = screenVector.Y;
            }

            OxyRect plotArea;
            switch (x.Unit)
            {
                case PlotLengthUnit.Data:
                    switch (y.Unit)
                    {
                        case PlotLengthUnit.Data:
                            return new ScreenVector(x1, y1);
                        case PlotLengthUnit.ScreenUnits:
                            y1 = y.Value;
                            break;
                        case PlotLengthUnit.RelativeToViewport:
                            y1 = model.Height * y.Value;
                            break;
                        case PlotLengthUnit.RelativeToPlotArea:
                            plotArea = model.PlotArea;
                            y1 = plotArea.Height * y.Value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    return new ScreenVector(x1, y1);

                case PlotLengthUnit.ScreenUnits:
                    x1 = x.Value;
                    break;

                case PlotLengthUnit.RelativeToViewport:
                    x1 = model.Width * x.Value;
                    break;

                case PlotLengthUnit.RelativeToPlotArea:
                    plotArea = model.PlotArea;
                    x1 = plotArea.Width * x.Value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (y.Unit)
            {
                case PlotLengthUnit.Data:
                    return new ScreenVector(x1, y1);
                case PlotLengthUnit.ScreenUnits:
                    return new ScreenVector(x1, y.Value);
                case PlotLengthUnit.RelativeToViewport:
                    return new ScreenVector(x1, model.Height * y.Value);
                case PlotLengthUnit.RelativeToPlotArea:
                    plotArea = model.PlotArea;
                    return new ScreenVector(x1, plotArea.Height * y.Value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public override OxyRect GetClippingRect()
        {
            return (X.Unit == PlotLengthUnit.Data || Y.Unit == PlotLengthUnit.Data) ?
                base.GetClippingRect() :
                OxyRect.Everything;
        }


        protected bool isPressed;
        private OxyColor _textColor;


        protected override void OnMouseDown(OxyMouseDownEventArgs args)
        {
            if (actualBounds.Contains(args.Position))
            {
                isPressed = true;
                args.Handled = true;
                PlotModel?.InvalidatePlot(false);
            }
            base.OnMouseDown(args);
        }

        protected override void OnMouseUp(OxyMouseEventArgs args)
        {
            if (isPressed)
            {
                isPressed = false;
                args.Handled = true;
                PlotModel?.InvalidatePlot(false);
            }
            base.OnMouseUp(args);
        }


        #endregion

        public static readonly Dictionary<string, OxyImage> IconCache = new Dictionary<string, OxyImage>();
        ///// <summary>
        ///// 在 Avalonia 平台上将任意文字渲染为 OxyImage（PNG 格式的字形图像）。
        ///// 使用 Avalonia 的 FormattedText 构造函数（string, CultureInfo, FlowDirection, Typeface, double, IBrush?）。
        ///// </summary>
        ///// <param name="text">要渲染的文字内容。</param>
        ///// <param name="foreground">文字颜色（OxyColor）。</param>
        ///// <param name="background">背景颜色（OxyColor）。</param>
        ///// <param name="fontFamily">字体族名称，例如 "Segoe UI" 或 自定义 IconFont 的 Key。</param>
        ///// <param name="fontSize">初始字体大小（默认 24）。实际渲染时会动态计算一个可填满画布又不超出的最优值。</param>
        ///// <returns>渲染好的 OxyImage，可直接用于 OxyPlot。</returns>
        //public static OxyImage RenderTextToOxyImage(
        //    string text,
        //    OxyColor foreground,
        //    OxyColor background,
        //    string fontFamily,
        //    double fontSize = 24)
        //{
        //    // 1. 定义渲染画布尺寸：为了保证清晰度，至少 64×64，或者 fontSize×3（视情况而定）
        //    double renderSize = Math.Max(fontSize * 3, 64);
        //    int pxSize = (int)Math.Ceiling(renderSize);

        //    // 2. 将 OxyColor 转为 Avalonia 的 Color
        //    Color fgColor = Color.FromArgb(foreground.A, foreground.R, foreground.G, foreground.B);
        //    Color bgColor = Color.FromArgb(background.A, background.R, background.G, background.B);
        //    var fgBrush = new SolidColorBrush(fgColor);
        //    var bgBrush = new SolidColorBrush(bgColor);

        //    // 3. 创建 RenderTargetBitmap，在 Avalonia 中用于离屏绘制
        //    var pixelSize = new PixelSize(pxSize, pxSize);
        //    var dpi = new Vector(96, 96); // 使用 96 DPI，与 WPF 默认保持一致
        //    var rtb = new RenderTargetBitmap(pixelSize, dpi);

        //    // 4. 在 RenderTargetBitmap 上进行绘制：先填充背景，再绘制文字
        //    using (var ctx = rtb.CreateDrawingContext())
        //    {
        //        // 4.1 绘制纯色背景矩形
        //        ctx.FillRectangle(bgBrush, new Rect(0, 0, renderSize, renderSize));

        //        // 4.2 准备 Typeface，这里用传入的 fontFamily 字符串（可以是系统字体，也可以是通过 resm: 引用的内嵌字体）
        //        var typeface = new Typeface(new FontFamily(fontFamily));

        //        // 4.3 第一次测量：用初始 fontSize 创建 FormattedText，测量它在该字号下的实际宽高
        //        var ftTest = new FormattedText(
        //            text,
        //            CultureInfo.CurrentCulture,
        //            FlowDirection.LeftToRight,
        //            typeface,
        //            fontSize,
        //            fgBrush)
        //        {
        //            // 约束最大宽高，如果文字本身超出 renderSize，则会换行/截断，这里只是为了测量 Bounds
        //            MaxTextWidth = renderSize,
        //            MaxTextHeight = renderSize
        //        };

        //        // 4.4 使用 TextSize.Width/Height 获取测量结果
        //        double measuredWidth = ftTest.TextSize.Width;
        //        double measuredHeight = ftTest.TextSize.Height;

        //        // 4.5 计算放缩比例：取 renderSize/测量宽度 和 renderSize/测量高度 的最小值
        //        double wRatio = renderSize / measuredWidth;
        //        double hRatio = renderSize / measuredHeight;
        //        double bestFontSize = fontSize * Math.Min(wRatio, hRatio);

        //        // 4.6 用最优字号重新创建 FormattedText
        //        var ft = new FormattedText(
        //            text,
        //            CultureInfo.CurrentCulture,
        //            FlowDirection.LeftToRight,
        //            typeface,
        //            bestFontSize,
        //            fgBrush)
        //        {
        //            MaxTextWidth = renderSize,
        //            MaxTextHeight = renderSize
        //        };

        //        // 4.7 再次获取新的宽高
        //        double finalWidth = ft.TextSize.Width;
        //        double finalHeight = ft.TextSize.Height;

        //        // 4.8 计算居中绘制的偏移量
        //        double textX = (renderSize - finalWidth) / 2.0;
        //        double textY = (renderSize - finalHeight) / 2.0;

        //        // 4.9 真正把文字绘制到画布上
        //        ctx.DrawText(fgBrush, new Point(textX, textY), ft);
        //    }

        //    // 5. 将离屏 Bitmap 保存为 PNG（调用 RenderTargetBitmap.Save(Stream)），返回 OxyImage
        //    using (var ms = new MemoryStream())
        //    {
        //        rtb.Save(ms); // 将 RenderTargetBitmap 直接保存为 PNG
        //        return new OxyImage(ms.ToArray());
        //    }
        //}
        public static OxyImage RenderTextToOxyImage(
            string text,
            OxyColor foreground,
            OxyColor background,
            string fontFamily,
            double fontSize = 24)
        {
            // 计算渲染尺寸（建议至少为字体大小的3倍）
            double renderSize = Math.Max(fontSize * 3, 64);
            int pixelSize = (int)Math.Ceiling(renderSize);

            // 创建位图
            using (var renderTarget = new RenderTargetBitmap(new PixelSize(pixelSize, pixelSize)))
            using (var drawingContext = renderTarget.CreateDrawingContext())
            {
                // 获取字体资源
                FontFamily iconFont = new FontFamily("avares://SCSA.Plot/Assets/Fonts/iconfont.ttf#iconfont");

                // 绘制背景
                var backgroundBrush = background.ToBrush();
                drawingContext.FillRectangle(backgroundBrush, new Rect(0, 0, renderSize, renderSize));

                // 创建Typeface
                var typeface = new Typeface(iconFont);

                // 创建临时FormattedText进行测量
                var testText = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    foreground.ToBrush())
                {
                    // 约束最大宽高，如果文字本身超出 renderSize，则会换行/截断，这里只是为了测量 Bounds
                    MaxTextWidth = renderSize,
                    MaxTextHeight = renderSize
                };


                // 计算缩放比例并留10%的边距
                double widthRatio = renderSize / testText.Width;
                double heightRatio = renderSize / testText.Height;
                var bestFontSize = fontSize * Math.Min(widthRatio, heightRatio) * 0.9;
  
                var formattedText = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    bestFontSize,
                    foreground.ToBrush())
                {
                    // 约束最大宽高，如果文字本身超出 renderSize，则会换行/截断，这里只是为了测量 Bounds
                    MaxTextWidth = renderSize,
                    MaxTextHeight = renderSize
                };

                // 计算居中位置
                var position = new Point(
                    (renderSize - formattedText.Width) / 2,
                    (renderSize - formattedText.Height) / 2);

                // 使用正确的DrawText方法绘制文本
                drawingContext.DrawText(formattedText, position);

                // 转换为OxyImage
                using (var memoryStream = new MemoryStream())
                {
                    renderTarget.Save(memoryStream);
                    return new OxyImage(memoryStream.ToArray());
                }
            }
        }



        public override void Render(IRenderContext rc)
        {
            //base.Render(rc);

            // calculate position and size
            var pos = GetPoint(X, Y, PlotModel) + GetVector(OffsetX, OffsetY, PlotModel);
            var sz = GetVector(Width, Height, PlotModel);
            double w = Math.Abs(sz.X), h = Math.Abs(sz.Y);
            double x = pos.X, y = pos.Y;
            // align
            if (HorizontalAlignment == HorizontalAlignment.Center) x -= w / 2;
            else if (HorizontalAlignment == HorizontalAlignment.Right) x -= w;
            if (VerticalAlignment == VerticalAlignment.Middle) y -= h / 2;
            else if (VerticalAlignment == VerticalAlignment.Bottom) y -= h;

            actualBounds = new OxyRect(x, y, w, h);


            if (!string.IsNullOrWhiteSpace(Text))
            {
                // 1) 确定内边距，比如离右侧留 4px
                double padding = 20;

                // 2) 先算出按钮框的右边坐标
                double rightX = x + w;


                // 4) 围绕矩形框的垂直中心，保持垂直居中
                double textY = y + h / 2;

                // 5) 把文字的“参照点”放在 (rightX - padding, textY)，并且用 HorizontalAlignment.Right
                var rightPoint = new ScreenPoint(rightX - padding, textY);

                rc.DrawText(
                    rightPoint,
                    Text,
                    TextColor,
                    PlotModel.DefaultFont,         // 不指定字体，则用默认
                    FontSize,
                    FontWeight,
                    0,            // 旋转角度
                    HorizontalAlignment.Right,
                    VerticalAlignment.Middle
                );
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

    public static class OxyColorExtensions
    {
        public static OxyColor ChangeIntensity(this OxyColor c, double factor)
        {
            return OxyColor.FromArgb(c.A,
                (byte)(c.R * factor),
                (byte)(c.G * factor),
                (byte)(c.B * factor));
        }
    }
}
