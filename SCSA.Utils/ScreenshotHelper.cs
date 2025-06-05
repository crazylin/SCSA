using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCSA.Utils
{
    public static class ScreenshotHelper
    {
        /// <summary>
        /// 捕获并保存指定控件为 PNG 文件。必须传入控件所在的 Window 以确保能够正确获取 RenderScaling。
        /// </summary>
        /// <param name="control">要截图的 Avalonia 控件。</param>
        /// <param name="owner">控件所在的 Window，用于获取 RenderScaling 并作为文件对话框的所属窗口。</param>
        public static async Task CaptureAndSaveControlAsync(Control control, Avalonia.Controls.Window owner)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            // 1. 弹出“保存文件”对话框
            var saveOptions = new FilePickerSaveOptions
            {
                Title = "保存截图",
                DefaultExtension = "png",
                // 以下部分演示如何正确给 Patterns 赋值：
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("PNG 图片")
                    {
                        // 必须用 new List<string> 或 new[] 显式创建一个列表/数组
                        Patterns = new List<string> { "*.png" }
                    }
                }
            };

            var file = await owner.StorageProvider.SaveFilePickerAsync(saveOptions);
            if (file == null)
            {
                // 用户取消或对话框关闭
                return;
            }

            // 2. 获取 Window 的 RenderScaling 作为 DPI 缩放因子
            //    不再依赖 control.VisualRoot，直接用传入的 owner。
            double scale = owner.RenderScaling;

            // 3. 获取控件的逻辑尺寸（Bounds），并计算物理像素大小
            //    注意：必须保证控件已经完成 Measure/Arrange，否则 Bounds 可能是 0。
            var bounds = control.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                // 控件尚未布局完成或尺寸为 0，无法截图
                // 这里可以自行决定是否抛异常或弹框提示。示例里直接返回。
                return;
            }

            int pixelWidth = (int)(bounds.Width * scale);
            int pixelHeight = (int)(bounds.Height * scale);
            var pixelSize = new PixelSize(pixelWidth, pixelHeight);

            // 4. 构造 RenderTargetBitmap 并渲染控件
            //    DPI 向量 = (96 * scale, 96 * scale)
            var dpiVector = new Vector(96 * scale, 96 * scale);
            var renderBitmap = new RenderTargetBitmap(pixelSize, dpiVector);

            // 确保控件已经 Measure/Arrange（如果控件是可见的，一般不必重复）
            control.Measure(bounds.Size);
            control.Arrange(new Rect(bounds.Size));

            renderBitmap.Render(control);

            // 5. 保存到文件
            try
            {
                await using var writeStream = await file.OpenWriteAsync();
                renderBitmap.Save(writeStream); // 默认保存为 PNG

                // 如果需要弹框提示“保存成功”，可引入 MessageBox.Avalonia 之类的库：
                // await MessageBox.Avalonia.MessageBoxManager
                //     .GetMessageBoxStandardWindow("成功", "截图已保存！")
                //     .ShowDialog(owner);
            }
            catch (Exception ex)
            {
                // 保存失败时提示错误
                // await MessageBox.Avalonia.MessageBoxManager
                //     .GetMessageBoxStandardWindow("错误", $"保存失败: {ex.Message}")
                //     .ShowDialog(owner);
            }
        }
    }
}
