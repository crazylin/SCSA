using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Axes;

namespace SCSA
{
    //public static class BlueTheme
    //{
    //    public static void Apply(PlotModel plotModel)
    //    {
    //        // 主背景和文本颜色
    //        //plotModel.Background = OxyColor.FromRgb(240, 245, 255);  // 浅蓝色背景
    //        plotModel.TextColor = OxyColor.FromRgb(0, 50, 100);       // 深蓝色文本
    //        plotModel.PlotAreaBorderColor = OxyColor.FromRgb(200, 220, 240);
    //        plotModel.PlotAreaBackground = OxyColors.White;
    //        plotModel.TitleColor = OxyColor.FromRgb(0, 80, 160);     // 标题使用更深的蓝色

    //        // 应用到所有轴
    //        foreach (var axis in plotModel.Axes)
    //        {
    //            // 通用轴设置
    //            axis.AxislineColor = OxyColor.FromRgb(0, 100, 200);
    //            axis.TicklineColor = OxyColor.FromRgb(0, 100, 200);
    //            axis.MinorTicklineColor = OxyColor.FromRgb(150, 190, 220);
    //            axis.TextColor = OxyColor.FromRgb(0, 50, 100);
    //            axis.TitleColor = OxyColor.FromRgb(0, 80, 160);

    //            // 网格线设置
    //            axis.MajorGridlineColor = OxyColor.FromArgb(40, 0, 100, 200);
    //            axis.MinorGridlineColor = OxyColor.FromArgb(20, 150, 190, 220);
    //            //axis.MajorGridlineStyle = LineStyle.Solid;
    //            //axis.MinorGridlineStyle = LineStyle.Dot;

    //            // 如果是线性轴，可以添加更多设置
    //            if (axis is LinearAxis linearAxis)
    //            {
    //                linearAxis.ExtraGridlineColor = OxyColor.FromRgb(0, 150, 255);
    //            }
    //        }

    //        // 应用到现有系列
    //        var bluePalette = new[]
    //        {
    //        OxyColor.FromRgb(0, 100, 200),     // 主蓝色
    //        OxyColor.FromRgb(0, 150, 255),     // 亮蓝色
    //        OxyColor.FromRgb(0, 180, 210),     // 青蓝色
    //        OxyColor.FromRgb(100, 180, 255),   // 浅蓝色
    //        OxyColor.FromRgb(0, 80, 160)       // 深蓝色
    //    };

    //        int colorIndex = 0;
    //        foreach (var series in plotModel.Series)
    //        {
    //            // 线系列
    //            if (series is LineSeries lineSeries)
    //            {
    //                lineSeries.Color = bluePalette[colorIndex % bluePalette.Length];
    //                //lineSeries.StrokeThickness = 2;
    //                //lineSeries.MarkerType = MarkerType.Circle;
    //                //lineSeries.MarkerSize = 4;
    //                lineSeries.MarkerFill = lineSeries.Color;
    //                lineSeries.MarkerStroke = OxyColors.White;
    //                //lineSeries.MarkerStrokeThickness = 1;
    //            }
    //            // 柱状图系列
    //            //else if (series is ColumnSeries columnSeries)
    //            //{
    //            //    columnSeries.FillColor = bluePalette[colorIndex % bluePalette.Length].ChangeIntensity(0.7);
    //            //    columnSeries.StrokeColor = OxyColor.FromRgb(0, 80, 160);
    //            //    columnSeries.StrokeThickness = 1;
    //            //}

    //            colorIndex++;
    //        }

    //        foreach(var legend in plotModel.Legends)
    //        {
    //            legend.TextColor = plotModel.TextColor;
    //            legend.LegendTitleColor = plotModel.TitleColor;
    //            legend.LegendBackground = OxyColor.FromArgb(200, 240, 245, 255);
    //            legend.LegendBorder = OxyColor.FromRgb(200, 220, 240);
    //        }
         
          
    //    }
    //}
}
