using OxyPlot.Annotations;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickMA.Modules.Plot
{

    public class IconButtonAnnotation : Annotation
    {
        public Action ClickAction { get; set; }
        public OxyImage Icon { get; set; }
        public OxyImage ToggledIcon { get; set; }  // 切换状态时的图标
        public double Width { get; set; } = 24;
        public double Height { get; set; } = 24;

        public double X { get; set; } // 添加 X 属性
        public double Y { get; set; } // 添加 Y 属性
        public OxyRect ScreenRectangle { get; private set; }
        public bool IsToggled { get; set; }
        public string Label { get; set; } // 简单标签，如 "0" 或 "S"

        public Action<IRenderContext, OxyRect> CustomRender { get; set; }

        public override void Render(IRenderContext rc)
        {
            // 计算图标屏幕位置
            var sp = new ScreenPoint(this.X, this.Y);
            var rect = new OxyRect(
                sp.X - Width / 2,
                sp.Y - Height / 2,
                Width,
                Height);
            ScreenRectangle = rect;
            if (CustomRender != null)
            {
                CustomRender(rc, rect);
            }
            else if (Icon != null)
            {
                var iconToDraw = IsToggled ? ToggledIcon : Icon;
                rc.DrawImage(iconToDraw, rect.Left, rect.Top, rect.Width, rect.Height, 1.0, true);
            }
            else
            {
                rc.DrawRectangle(rect, IsToggled ? OxyColors.Gray : OxyColors.LightGray, OxyColors.Black, 1,
                    EdgeRenderingMode.Automatic);

                if (!string.IsNullOrEmpty(Label))
                {
                    rc.DrawText(
                        new ScreenPoint(rect.Center.X, rect.Center.Y),
                        Label,
                        OxyColors.Black,
                        fontSize: 12,
                        horizontalAlignment: HorizontalAlignment.Center,
                        verticalAlignment: VerticalAlignment.Middle);
                }
            }
           
        }

    }

}
