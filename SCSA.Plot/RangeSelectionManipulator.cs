using OxyPlot.Annotations;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia.Input;
using OxyPlot.Avalonia;
using LineSeries = OxyPlot.Series.LineSeries;
using RectangleAnnotation = OxyPlot.Annotations.RectangleAnnotation;


namespace QuickMA.Modules.Plot
{

    /// <summary>
    /// Enables range selection with creating, resizing and dragging of a RectangleAnnotation.
    /// </summary>
    public class RangeSelectionManipulator : MouseManipulator
    {
        private enum Mode { None, Creating, ResizingLeft, ResizingRight, Dragging }
        private Mode _mode;
        private DataPoint _anchorPoint;
        private RectangleAnnotation _rect;
        private DataPoint _dragStart;
        private double _origMinX, _origMaxX;
        private const double EdgeTolerance = 6; // pixels

        private readonly PlotView _wpfPlotView;
        public static bool _isAdd;

        public RangeSelectionManipulator(IPlotView plotView)
            : base(plotView)
        {
            _wpfPlotView = plotView as PlotView;
            if (_wpfPlotView != null && !_isAdd)
            {
                _isAdd = true;
                _wpfPlotView.PointerMoved += _wpfPlotView_PointerMoved;
            }
        }

        private void _wpfPlotView_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            var model = PlotView.ActualModel;
            if (model == null)
            {
                
                _wpfPlotView.Cursor = new Cursor(StandardCursorType.Arrow);
                return;
            }

            var rect = model.Annotations
                .OfType<RectangleAnnotation>()
                .FirstOrDefault(a => a.Tag?.ToString() == "RangeSelect");
            var pos = e.GetPosition(_wpfPlotView);

            if (rect == null || _mode == Mode.Creating)
            {
                _wpfPlotView.Cursor = new Cursor(StandardCursorType.Arrow);
                return;
            }

            var xAxis = model.DefaultXAxis;
            double leftX = xAxis.Transform(rect.MinimumX);
            double rightX = xAxis.Transform(rect.MaximumX);

            // inside rect for dragging
            if (pos.X > leftX + EdgeTolerance && pos.X < rightX - EdgeTolerance)
            {
                _wpfPlotView.Cursor = new Cursor(StandardCursorType.SizeAll);
            }
            else if (Math.Abs(pos.X - leftX) < EdgeTolerance || Math.Abs(pos.X - rightX) < EdgeTolerance)
            {
                _wpfPlotView.Cursor = new Cursor(StandardCursorType.SizeWestEast);
            }
            else
            {
                _wpfPlotView.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }


        public override void Started(OxyMouseEventArgs e)
        {
            base.Started(e);
            var model = PlotView.ActualModel;
            if (model == null) return;

            _rect = model.Annotations
                .OfType<RectangleAnnotation>()
                .FirstOrDefault(a => a.Tag?.ToString() == "RangeSelect");

            var xData = model.DefaultXAxis.InverseTransform(e.Position.X);
            bool insideRect = false;

            if (_rect != null)
            {
                double leftX = model.DefaultXAxis.Transform(_rect.MinimumX);
                double rightX = model.DefaultXAxis.Transform(_rect.MaximumX);
                insideRect = e.Position.X > leftX + EdgeTolerance && e.Position.X < rightX - EdgeTolerance;

                if (Math.Abs(e.Position.X - leftX) < EdgeTolerance)
                    _mode = Mode.ResizingLeft;
                else if (Math.Abs(e.Position.X - rightX) < EdgeTolerance)
                    _mode = Mode.ResizingRight;
                else if (insideRect)
                    _mode = Mode.Dragging;
                else
                {
                    model.Annotations.Remove(_rect);
                    _rect = null;
                    _mode = Mode.Creating;
                }
            }
            else
            {
                _mode = Mode.Creating;
            }

            if (_mode == Mode.Creating)
            {
                _anchorPoint = new DataPoint(xData, 0);
                _rect = new RectangleAnnotation
                {
                    Tag = "RangeSelect",
                    Fill = OxyColor.FromAColor(60, OxyColors.SkyBlue),
                    Layer = AnnotationLayer.AboveSeries,
                    StrokeThickness = 1,
                    MinimumY = model.DefaultYAxis.ActualMinimum,
                    MaximumY = model.DefaultYAxis.ActualMaximum
                };
                model.Annotations.Add(_rect);
            }
            else if (_mode == Mode.Dragging)
            {
                _dragStart = new DataPoint(xData, 0);
                _origMinX = _rect.MinimumX;
                _origMaxX = _rect.MaximumX;
            }

            e.Handled = true;
        }

        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);
            if (_mode == Mode.None || _rect == null)
                return;

            var model = PlotView.ActualModel;
            double currentX = model.DefaultXAxis.InverseTransform(e.Position.X);

            switch (_mode)
            {
                case Mode.Creating:
                    _rect.MinimumX = Math.Min(_anchorPoint.X, currentX);
                    _rect.MaximumX = Math.Max(_anchorPoint.X, currentX);
                    break;
                case Mode.ResizingLeft:
                    _rect.MinimumX = Math.Min(currentX, _rect.MaximumX);
                    break;
                case Mode.ResizingRight:
                    _rect.MaximumX = Math.Max(currentX, _rect.MinimumX);
                    break;
                case Mode.Dragging:
                    double delta = currentX - _dragStart.X;
                    _rect.MinimumX = _origMinX + delta;
                    _rect.MaximumX = _origMaxX + delta;
                    break;
            }

            model.InvalidatePlot(false);
            e.Handled = true;
        }

        public override void Completed(OxyMouseEventArgs e)
        {
            base.Completed(e);
            _mode = Mode.None;
            e.Handled = true;



            var model = ((CuPlotModel)PlotView.ActualModel);
            var series = model.Series.Where(ser=>ser.GetType()==typeof(LineSeries)).Cast<LineSeries>().ToList();

            var rangeResults = new List<RangeResult>();
            foreach (var lineSeries in series)
            {
                try
                {
                    var (leftDp, rightDp, peakDp, meanValue) = GetSegmentExtrema(lineSeries, _rect.MinimumX, _rect.MaximumX);

                    var rangeResult = new RangeResult()
                    {
                        LeftPoint = leftDp,
                        RightPoint = rightDp,
                        PeakPoint = peakDp,
                        MeanValue = meanValue,
                        RmsValue = peakDp.Y * 2.0 / Math.Sqrt(2),

                        XUint = model.XUint,
                        YUint = model.YUnit,
                        XTitle = model.XTitle,
                        YTitle = model.YTitle,
                    };
                    if (!string.IsNullOrWhiteSpace(lineSeries.Title))
                    {
                        rangeResult.YTitle = $"{lineSeries.Title}";
                    }
                    else
                    {
                        rangeResult.YTitle = rangeResult.YTitle;
                    }

                    rangeResults.Add(rangeResult);
                }
                catch (Exception exception)
                {

                }
            }


            model.RangeResults = rangeResults;

        }



        /// <summary>
        /// 给定一条折线（LineSeries）和 [minX, maxX] 范围，
        /// 1. 在 minX 处插值得到 Y，组成 DataPoint(minX, Ymin)；
        /// 2. 在 maxX 处插值得到 Y，组成 DataPoint(maxX, Ymax)；
        /// 3. 在原始的 LineSeries.Points 中，找出所有 X 在 [minX, maxX] 范围内（含端点）的点，并返回其中 Y 值最大的那个 DataPoint。
        /// </summary>
        /// <param name="series">要查询的 LineSeries（必须确保 Points 已按 X 升序排好，并且 PlotModel 已经完成布局）</param>
        /// <param name="minX">区间左端点（数据坐标系）</param>
        /// <param name="maxX">区间右端点（数据坐标系）</param>
        /// <returns>
        /// 返回一个元组：(DataPoint leftPoint, DataPoint rightPoint, DataPoint peakPoint)
        ///  - leftPoint：在 minX 处插值得到的点 (minX, Ymin)
        ///  - rightPoint：在 maxX 处插值得到的点 (maxX, Ymax)
        ///  - peakPoint：在原始 series.Points 中 X∈[minX,maxX] 的所有点里，Y 最大的那个实际采样点（DataPoint）
        /// </returns>
        public static (DataPoint leftPoint, DataPoint rightPoint, DataPoint peakPoint,double meanValue)
            GetSegmentExtrema(LineSeries series, double minX, double maxX)
        {

            var points = series.ItemsSource as IEnumerable<DataPoint>;
            if (points == null || !points.Any())
                throw new InvalidOperationException("LineSeries 中没有任何采样点，无法插值和求最大值。");
            points = points.ToList();
            // 确保 minX <= maxX，若输入反了则交换
            if (minX > maxX)
            {
                var tmp = minX;
                minX = maxX;
                maxX = tmp;
            }

            // 1) 在 minX 处做线性插值，得到 Ymin
            double ymin = InterpolateY(points, minX);

            // 2) 在 maxX 处做线性插值，得到 Ymax
            double ymax = InterpolateY(points, maxX);

            var leftPoint = new DataPoint(minX, ymin);
            var rightPoint = new DataPoint(maxX, ymax);

            // 3) 在原始 Points 里找出所有 X ∈ [minX, maxX] 的点，返回其中 Y 最大的那个
            // （如果恰好有多个相同 Y，也取第一个即可）
            points = points
                .Where(p => p.X >= minX && p.X <= maxX)
                .OrderByDescending(p => p.Y);
            DataPoint peakPoint = points.First();

            double meanValue = points.Sum(p => p.Y) / points.Count();

            return (leftPoint, rightPoint, peakPoint, meanValue);
        }

        /// <summary>
        /// 对一个按 X 升序排列的 DataPoint 集合进行线性插值：
        /// 给定 dataX，找到 points 里相邻的 (x0, y0)、(x1, y1)，
        /// 当 dataX ∈ [x0, x1] 时做线性插值；如果在最左端或最右端外则钳制到端点值。
        /// </summary>
        private static double InterpolateY(IEnumerable<DataPoint> p, double dataX)
        {
            // 边界处理：若 dataX 在最左端之外，则返回第一个点的 Y
            var points = p.ToList();
            if (dataX <= points[0].X)
                return points[0].Y;

            // 若 dataX 在最右端之外，则返回最后一个点的 Y
            int lastIndex = points.Count - 1;
            if (dataX >= points[lastIndex].X)
                return points[lastIndex].Y;

            // 否则 dataX 在中间某个相邻区间内，循环查找并做插值
            for (int i = 0; i < lastIndex; i++)
            {
                double x0 = points[i].X;
                double x1 = points[i + 1].X;
                if (dataX >= x0 && dataX <= x1)
                {
                    double y0 = points[i].Y;
                    double y1 = points[i + 1].Y;
                    if (Math.Abs(x1 - x0) < 1e-12)
                    {
                        // 防止除零：如果 x0≈x1，则直接取 y0
                        return y0;
                    }
                    double t = (dataX - x0) / (x1 - x0);
                    return y0 + t * (y1 - y0);
                }
            }

            // 正常不会走到这里，抛异常提醒：
            throw new InvalidOperationException("插值时未找到 dataX 所在的区间。");
        }

    }
    public class RangeResult
    {
        public DataPoint LeftPoint { set; get; }

        public DataPoint RightPoint { set; get; }

        public DataPoint PeakPoint { set; get; }

        public double MeanValue { set; get; }

        public double RmsValue { set; get; }

        public string XUint { set; get; }

        public string YUint { set; get; }

        public string XTitle { set; get; }

        public string YTitle { set; get; }


    }
}
