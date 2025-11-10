using BluesReporter.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.TickGenerators;
using SkiaSharp;
using System.Collections;


namespace BluesReporter.Charts;

public static class ReportChartHelper
{
    public static IContainer AddDynamicChart(this IContainer container, List<ChartConfig> chartConfig, object data)
    {
        IPalette palette = new Category20();
        LabelStyle.RTLSupport = true;
        var dataList = data as IList;


        int count = chartConfig.Count;
        if (count == 0) return container;

        if (count == 1)
        {
            var svg = GenerateChartSvg(chartConfig[0], dataList!, palette, isFullWidth: true);
            container.AlignCenter()
                     .AlignMiddle()
                     .PaddingVertical(20)
                     .Svg(svg)
                     .FitWidth();

            return container;
        }
        container.Column(column =>
        {
            for (int i = 0; i < count; i += 2)
            {
                column.Item().Row(row =>
                {
                    var config1 = chartConfig[i];
                    var svg1 = GenerateChartSvg(config1, dataList!, palette, isFullWidth: false);
                    row.RelativeItem().Element(el => el.Svg(svg1).FitWidth());

                    if (i + 1 < count)
                    {
                        var config2 = chartConfig[i + 1];
                        var svg2 = GenerateChartSvg(config2, dataList!, palette, isFullWidth: false);
                        row.RelativeItem().Element(el => el.Svg(svg2).FitWidth());
                    }
                    else
                    {
                        row.RelativeItem().AlignCenter().Svg(svg1).FitWidth();
                    }
                });

            }
        });

        return container;
    }

    private static string GenerateChartSvg(ChartConfig config, IList dataList, IPalette palette, bool isFullWidth)
    {
        var plot = new Plot();
        plot.Font.Set(config.FontName);
        plot.Title(config.Title);
        plot.Axes.Bottom.Label.Text = config.XLabel;
        plot.Axes.Left.Label.Text = config.YLabel;

        if (config.ShowLegend)
        {
            plot.ShowLegend(config.LegendAlign);
            plot.Legend.Orientation = config.Orientation;
            plot.Legend.FontSize = 10;
        }

        var list = dataList!.Cast<object>();
        var labels = list.Select(item => item?.GetType().GetProperty(config.XValue)?.GetValue(item)?.ToString() ?? "نامشخص").ToArray();

        var values = list.Select(item => Convert.ToDouble(item?.GetType().GetProperty(config.YValue)?.GetValue(item) ?? 0))
            .ToArray();

    
        switch (config.ChartType.ToLower())
        {
            case "bar":
                {
                    var barPlot = plot.Add.Bars(values);

                    //plot.Legend.ManualItems.Clear();
                    for (int i = 0; i < barPlot.Bars.Count; i++)
                    {
                        barPlot.Bars[i].FillColor = palette.GetColor(i);

                        plot.Legend.ManualItems.Add(new LegendItem
                        {
                            LabelText = labels[i],
                            FillColor = palette.GetColor(i),
                        });
                    }

                    if (config.ShowValueLable)
                    {
                        foreach (var bar in barPlot.Bars)
                        {
                            bar.Size = .4;

                            bar.Label = bar.Value.ToString(config.FormattingText);
                        }
                    }

                    Tick[] ticks = labels.Select((label, i) => new Tick(i, label)).ToArray();
                    plot.Axes.Bottom.TickGenerator = new NumericManual(ticks);

                    plot.Axes.Bottom.MajorTickStyle.Length = 0; 
                    if (config.Rotate != 0)
                    {

                        plot.Axes.Bottom.TickLabelStyle.Rotation = config.Rotate;
                        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperRight;
                        plot.Axes.Bottom.TickLabelStyle.OffsetY = -13;
                        plot.Axes.Bottom.TickLabelStyle.OffsetX = 5;
                    }

                    if (values.Min() < 0)
                    {
                        plot.Axes.SetLimitsY(values.Min() * 1.5, values.Max() * 1.5);
                    }
                    else
                    {
                        plot.Axes.Margins(bottom: 0);
                    }
                    break;
                }
            case "pie":
                {
                    break;
                }
            case "line":
                {
                    break;
                }


        }


        return plot.GetSvgXml(500, 300);
    }
}

