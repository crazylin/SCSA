using OxyPlot;

namespace SCSA.Plot;

/// <summary>Represents a toggle button annotation with text and clickable behavior.</summary>
public class ButtonAnnotation : TextAnnotation
{
    public override void Render(IRenderContext rc)
    {
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
        var border = BorderThickness;
        if (isPressed)
        {
            fill = fill.ChangeIntensity(0.8);
            border += 1;
        }

        //border = 0;
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

            var iconWidth = w;
            var iconHeight = h;
            var iconX = x;
            var iconY = y;


            // 保持图标比例
            var aspectRatio = (double)iconImage.Width / iconImage.Height;
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

            if (isPressed)
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
            if (isPressed) center = new ScreenPoint(x + w / 2 + 1, y + h / 2 + 0.5);
            rc.DrawText(center, Text, TextColor, PlotModel.DefaultFont, FontSize, FontWeight,
                0, HorizontalAlignment.Center, VerticalAlignment.Middle);
        }
    }
}