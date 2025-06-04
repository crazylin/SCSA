

using OxyPlot;
using OxyPlot.Series;

namespace QuickMA.Modules.Plot
{
    public class XZoomRectangleManipulator(IPlotView plotView) : MouseManipulator(plotView)
    {
        /// <summary>The zoom rectangle.</summary>
        private OxyRect zoomRectangle;

        /// <summary>
        /// Gets or sets a value indicating whether zooming is enabled.
        /// </summary>
        private bool IsZoomEnabled { get; set; }

        /// <summary>Occurs when a manipulation is complete.</summary>
        /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Completed(OxyMouseEventArgs e)
        {
            base.Completed(e);
            if (!this.IsZoomEnabled)
                return;
            this.PlotView.SetCursorType(CursorType.Default);
            this.PlotView.HideZoomRectangle();
            if (this.zoomRectangle.Width > 10.0 && this.zoomRectangle.Height > 10.0)
            {
                DataPoint dataPoint1 = this.InverseTransform(this.zoomRectangle.Left, this.zoomRectangle.Top);
                DataPoint dataPoint2 = this.InverseTransform(this.zoomRectangle.Right, this.zoomRectangle.Bottom);
                if (this.XAxis != null)
                    this.XAxis.Zoom(dataPoint1.X, dataPoint2.X);

                double xMin = this.PlotView.ActualModel.DefaultXAxis.InverseTransform(zoomRectangle.Left);
                double xMax = this.PlotView.ActualModel.DefaultXAxis.InverseTransform(zoomRectangle.Right);
                var ys = this.PlotView.ActualModel.Series
                    .OfType<LineSeries>()
                    .Where(s=>s.ItemsSource!=null)
                    .SelectMany(s => s.ItemsSource as IEnumerable<DataPoint>)
                        .Where(p => p.X >= xMin && p.X <= xMax)
                        .Select(p => p.Y);
        
                if (ys.Any())
                {
                    
                    double yMin = ys.Min();
                    double yMax = ys.Max();

                    var sub = (yMax - yMin) * 0.1;

                    yMin = yMin - sub;
                    yMax = yMax + sub;

                    // 3. 把 Y 轴也缩放到这个区间
                    var yAxis = this.PlotView.ActualModel.DefaultYAxis;
                    yAxis.Zoom(yMin, yMax);
                }

                this.PlotView.InvalidatePlot();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Occurs when the input device changes position during a manipulation.
        /// </summary>
        /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Delta(OxyMouseEventArgs e)
        {
            base.Delta(e);
            if (!this.IsZoomEnabled)
                return;
            OxyRect plotArea = this.PlotView.ActualModel.PlotArea;
            double left = Math.Min(this.StartPosition.X, e.Position.X);
            double width = Math.Abs(this.StartPosition.X - e.Position.X);

            if (this.XAxis == null || !this.XAxis.IsZoomEnabled)
            {
                left = plotArea.Left;
                width = plotArea.Width;
            }

            //if (this.YAxis == null || !this.YAxis.IsZoomEnabled)
            //{
            //    top = plotArea.Top;
            //    height = plotArea.Height;
            //}

            this.zoomRectangle = new OxyRect(left, plotArea.Top, width, plotArea.Height);
            this.PlotView.ShowZoomRectangle(this.zoomRectangle);
            e.Handled = true;
        }

        /// <summary>
        /// Occurs when an input device begins a manipulation on the plot.
        /// </summary>
        /// <param name="e">The <see cref="T:OxyPlot.OxyMouseEventArgs" /> instance containing the event data.</param>
        public override void Started(OxyMouseEventArgs e)
        {
            base.Started(e);
            this.IsZoomEnabled = this.XAxis != null && this.XAxis.IsZoomEnabled ||
                                 this.YAxis != null && this.YAxis.IsZoomEnabled;
            if (!this.IsZoomEnabled)
                return;
            this.zoomRectangle = new OxyRect(this.StartPosition.X, this.StartPosition.Y, 0.0, 0.0);
            this.PlotView.ShowZoomRectangle(this.zoomRectangle);
            this.PlotView.SetCursorType(this.GetCursorType());
            e.Handled = true;
        }

        /// <summary>Gets the cursor for the manipulation.</summary>
        /// <returns>The cursor.</returns>
        private CursorType GetCursorType()
        {
            if (this.XAxis == null)
                return CursorType.ZoomVertical;
            return this.YAxis == null ? CursorType.ZoomHorizontal : CursorType.ZoomRectangle;
        }
    }
}
