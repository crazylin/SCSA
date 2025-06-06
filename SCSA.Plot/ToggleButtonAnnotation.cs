using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickMA.Modules.Plot
{
    public class ToggleButtonAnnotation: ButtonAnnotation
    {
        /// <summary>Occurs when the toggle state changes.</summary>
        public event EventHandler<ToggledEventArgs> Toggled;
        /// <summary>
        /// Raises the Toggled event.
        /// </summary>
        protected virtual void OnToggled(ToggledEventArgs e)
        {
            Toggled?.Invoke(this, e);
        }

        private bool isChecked;
        /// <summary>Gets or sets the toggle state.</summary>
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    if (value && !string.IsNullOrEmpty(GroupName) && PlotModel != null)
                    {
                        // uncheck others in group
                        var anns = PlotModel.Annotations.OfType<ToggleButtonAnnotation>().ToList();
                        foreach (var ann in anns)
                        {
                            if (ann != this && ann.GroupName == GroupName && ann.IsChecked)
                                ann.SetChecked(false);
                        }
                    }
                    SetChecked(value);
                }
            }
        }

        public void SetChecked(bool value)
        {
            isChecked = value;
            OnToggled(new ToggledEventArgs(value));
            PlotModel?.InvalidatePlot(false);
        }

        public string GroupName { get; set; } = string.Empty;
        protected override HitTestResult HitTestOverride(HitTestArguments args)
        {
            if (actualBounds.Contains(args.Point))
            {
                IsChecked = !IsChecked;
                return new HitTestResult(this, args.Point);
            }
            return null;
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

            // background
            var fill = Background;
            double border = BorderThickness;
            if (isPressed || isChecked)
            {
                fill = fill.ChangeIntensity(0.8);
                border += 1;

            }
            else
            {
                //border = 0;
            }
            rc.DrawRectangle(actualBounds, fill, Stroke, border, EdgeRenderingMode.Automatic);

            // draw icon or text

            if (!string.IsNullOrEmpty(IconText))
            {
                var iconKey = $"{IconText}_{IconFontFamily}_{IconFontSize}_{IconColor}";
                if (!IconCache.TryGetValue(iconKey, out var iconImage))
                {

                    iconImage = RenderTextToOxyImage(IconText, IconColor, OxyColors.Transparent, IconFontFamily,
                        IconFontSize);
                    IconCache[iconKey] = iconImage;
                }

                double iconWidth = w;
                double iconHeight = h;
                double iconX = x;
                double iconY = y;
                
                // 保持图标比例
                double aspectRatio = (double)iconImage.Width / iconImage.Height;
                if (iconWidth / iconHeight > aspectRatio)
                {
                    iconWidth = iconHeight * aspectRatio;
                    iconX = x + (w - iconWidth) / 2;
                }
                else
                {
                    iconHeight = iconWidth / aspectRatio;
                    iconY = y + (h - iconHeight) / 2;
                }

                if (isPressed || isChecked)
                {
                    iconX += 1;
                    iconY += 0.5;
                }

                rc.DrawImage(
                    iconImage,
                    iconX, iconY, iconWidth, iconHeight,
                    1, true);

                //rc.DrawText(center, IconText, IconColor, IconFontFamily, IconFontSize, FontWeights.Normal,
                //    0, HorizontalAlignment.Center, VerticalAlignment.Middle);
            }
            else if (!string.IsNullOrEmpty(Text))
            {
                var center = new ScreenPoint(x + w / 2, y + h / 2);
                if (isPressed || isChecked)
                {
                    center = new ScreenPoint(x + w / 2 + 1, y + h / 2 + 0.5);
                }
                rc.DrawText(center, Text, TextColor, PlotModel.DefaultFont, FontSize, FontWeight,
                    0, HorizontalAlignment.Center, VerticalAlignment.Middle);
            }
        }

    }    
    /// <summary>Provides data for the Toggled event.</summary>
    public class ToggledEventArgs : EventArgs
    {
        public ToggledEventArgs(bool isChecked)
        {
            IsChecked = isChecked;
        }

        public bool IsChecked { get; }
    }

}
