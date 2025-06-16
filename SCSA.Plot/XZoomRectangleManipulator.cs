using OxyPlot;
using OxyPlot.Series;

namespace SCSA.Plot;

public class XZoomRectangleManipulator(IPlotView plotView) : MouseManipulator(plotView)
{
    /// <summary>The zoom rectangle.</summary>
    private OxyRect zoomRectangle;

    /// <summary>
    ///     Gets or sets a value indicating whether zooming is enabled.
    /// </summary>
    private bool IsZoomEnabled { get; set; }

    /// <summary>Occurs when a manipulation is complete.</summary>
    /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
    public override void Completed(OxyMouseEventArgs e)
    {
        base.Completed(e);
        if (!IsZoomEnabled)
            return;
        PlotView.SetCursorType(CursorType.Default);
        PlotView.HideZoomRectangle();
        if (zoomRectangle.Width > 10.0 && zoomRectangle.Height > 10.0)
        {
            var dataPoint1 = InverseTransform(zoomRectangle.Left, zoomRectangle.Top);
            var dataPoint2 = InverseTransform(zoomRectangle.Right, zoomRectangle.Bottom);
            if (XAxis != null)
                XAxis.Zoom(dataPoint1.X, dataPoint2.X);

            var xMin = PlotView.ActualModel.DefaultXAxis.InverseTransform(zoomRectangle.Left);
            var xMax = PlotView.ActualModel.DefaultXAxis.InverseTransform(zoomRectangle.Right);
            var ys = PlotView.ActualModel.Series
                .OfType<LineSeries>()
                .Where(s => s.ItemsSource != null)
                .SelectMany(s => s.ItemsSource as IEnumerable<DataPoint>)
                .Where(p => p.X >= xMin && p.X <= xMax)
                .Select(p => p.Y);

            if (ys.Any())
            {
                var yMin = ys.Min();
                var yMax = ys.Max();

                var sub = (yMax - yMin) * 0.1;

                yMin = yMin - sub;
                yMax = yMax + sub;

                // 3. 把 Y 轴也缩放到这个区间
                var yAxis = PlotView.ActualModel.DefaultYAxis;
                yAxis.Zoom(yMin, yMax);
            }

            PlotView.InvalidatePlot();
        }

        e.Handled = true;
    }

    /// <summary>
    ///     Occurs when the input device changes position during a manipulation.
    /// </summary>
    /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
    public override void Delta(OxyMouseEventArgs e)
    {
        base.Delta(e);
        if (!IsZoomEnabled)
            return;
        var plotArea = PlotView.ActualModel.PlotArea;
        var left = Math.Min(StartPosition.X, e.Position.X);
        var width = Math.Abs(StartPosition.X - e.Position.X);

        if (XAxis == null || !XAxis.IsZoomEnabled)
        {
            left = plotArea.Left;
            width = plotArea.Width;
        }

        //if (this.YAxis == null || !this.YAxis.IsZoomEnabled)
        //{
        //    top = plotArea.Top;
        //    height = plotArea.Height;
        //}

        zoomRectangle = new OxyRect(left, plotArea.Top, width, plotArea.Height);
        PlotView.ShowZoomRectangle(zoomRectangle);
        e.Handled = true;
    }

    /// <summary>
    ///     Occurs when an input device begins a manipulation on the plot.
    /// </summary>
    /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
    public override void Started(OxyMouseEventArgs e)
    {
        base.Started(e);
        IsZoomEnabled = (XAxis != null && XAxis.IsZoomEnabled) ||
                        (YAxis != null && YAxis.IsZoomEnabled);
        if (!IsZoomEnabled)
            return;
        zoomRectangle = new OxyRect(StartPosition.X, StartPosition.Y, 0.0, 0.0);
        PlotView.ShowZoomRectangle(zoomRectangle);
        PlotView.SetCursorType(GetCursorType());
        e.Handled = true;
    }

    /// <summary>Gets the cursor for the manipulation.</summary>
    /// <returns>The cursor.</returns>
    private CursorType GetCursorType()
    {
        if (XAxis == null)
            return CursorType.ZoomVertical;
        return YAxis == null ? CursorType.ZoomHorizontal : CursorType.ZoomRectangle;
    }
}