using BluesReporter.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using SkiaSharp;
using System.Collections;
using System.Globalization;


namespace BluesReporter.Charts;

public static class ReportChartHelper
{
    public static IContainer AddDynamicChart(this IContainer container, List<ChartConfig> chartConfig, object data)
    {
        var dataList = data as IList;

        int count = chartConfig.Count;
        if (count == 0) return container;

        if (count == 1)
        {
            var svg = GenerateChartSvg(chartConfig[0], dataList!);
            container.AlignCenter().AlignMiddle()
                     .AlignMiddle()
                     .Svg(svg).FitArea();


            return container;
        }
        container.Column(column =>
        {
            for (int i = 0; i < count; i += 2)
            {
                column.Item().Row(row =>
                {
                    var config1 = chartConfig[i];
                    var svg1 = GenerateChartSvg(config1, dataList!);
                    row.RelativeItem().AlignCenter().AlignMiddle().Element(el => el.Svg(svg1).FitArea());

                    if (i + 1 < count)
                    {
                        var config2 = chartConfig[i + 1];
                        var svg2 = GenerateChartSvg(config2, dataList!);
                        row.RelativeItem().AlignCenter().AlignMiddle().Scale(2).Element(el => el.Svg(svg2).FitArea());
                    }
                    else
                    {
                        row.RelativeItem().AlignCenter().AlignMiddle().AlignCenter().Svg(svg1).FitArea();
                    }
                });

            }
        });

        return container;
    }

    private static string GenerateChartSvg(ChartConfig config, IList dataList)
    {
        IPalette palette = new Category20();
        LabelStyle.RTLSupport = true;
        var plot = new Plot();

        plot.Font.Set(config.FontName);
        plot.Title(config.Title, 12);
        plot.Axes.Bottom.Label.Text = config.XLabel;
        plot.Axes.Bottom.Label.FontSize = 12;
        plot.Axes.Left.Label.FontSize = 12;
        if (config.ShowLegend)
        {
            plot.ShowLegend(config.LegendAlign);
            plot.Legend.Orientation = config.Orientation;
            plot.Legend.FontSize = 10;
            plot.Legend.ShadowColor = Colors.White;
        }
        else
        {
            plot.HideLegend();

        }

        var list = dataList!.Cast<object>();
        var labels = list.Select(item => item?.GetType().GetProperty(config.XValue)?.GetValue(item)?.ToString() ?? "نامشخص").ToArray();
        var rawValues = list.Select(item => Convert.ToDouble(item?.GetType().GetProperty(config.YValue)?.GetValue(item) ?? 0)).ToArray();

        var numbers = ScaleNumbers(rawValues);
        var values = numbers.scaledNumbers;
        var scaleUnit = numbers.unit;

        plot.Axes.Left.Label.Text = string.IsNullOrEmpty(scaleUnit) ? config.YLabel : $"{config.YLabel} ({scaleUnit})";

        switch (config.ChartType.ToLower())
        {
            case "bar":
                {
                    var barPlot = plot.Add.Bars(values);

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
                            bar.Size = .5;
                            bar.Label = bar.Value.ToString(config.FormattingText);
                            bar.LabelOffset = -3;

                        }
                    }
                    

                    Tick[] ticks = labels.Select((label, i) => new Tick(i, label)).ToArray();
                    plot.Grid.YAxis.IsVisible = config.ShowYGrid;
                    plot.Grid.YAxis.IsVisible = config.ShowXGrid;
                    plot.Axes.Bottom.TickGenerator = new NumericManual(ticks);
                    plot.Axes.AutoScale();
                   
                    plot.Axes.Bottom.MajorTickStyle.Length = 0;
                    if (config.IsRotate)
                    {

                        plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
                        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperRight;
                        plot.Axes.Bottom.TickLabelStyle.OffsetY = -13;
                        plot.Axes.Bottom.TickLabelStyle.OffsetX = 5;
                        plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
                    }

                    if (values.Min() < 0)
                    {
                        plot.Axes.Margins(bottom: .3, top: .3);
                    }
                    else
                    {
                        plot.Axes.Margins(bottom: 0, top: .3);
                    }
                    break;
                }
            case "pie":
                {
                    var pieList = new List<PieSlice>();
                    for (int i = 0; i < values.Count(); i++)
                    {
                        if (values[i] > 0)
                        {
                            pieList.Add(new PieSlice
                            {
                                Value = values[i],
                                Label = labels[i],
                                FillColor = palette.GetColor(i)
                            });
                            plot.Legend.ManualItems.Add(new LegendItem
                            {
                                LabelText = labels[i],
                                FillColor = palette.GetColor(i),

                            });
                        }
                    }
                    var pie = plot.Add.Pie(pieList);
                    pie.DonutFraction = 0.5;
                    pie.LineColor = Colors.White;
                    pie.SliceLabelDistance = 1.5;
                    //if (config.ShowLegend)
                    //{
                    //    plot.ShowLegend(config.LegendAlign);
                    //    plot.Legend.Orientation = config.Orientation;
                    //    plot.Legend.FontSize = 10;
                    //    plot.Legend.ShadowColor = Colors.White;
                    //}
                    plot.HideAxesAndGrid();

                    break;
                }
            case "line":
                {

                    double[] xs = Generate.Consecutive(values.Length);
                    var line = plot.Add.Scatter(xs, values);

                    if (config.ShowValueLable)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            double x = xs[i];
                            double y = values[i];
                            double textY = y;
                            var txt = plot.Add.Text(y.ToString(config.FormattingText), x, textY);
                            txt.LabelFontSize = 11;
                            txt.OffsetY = -5;
                            txt.LabelAlignment = Alignment.LowerCenter;
                        }
                    }
                    line.LineWidth = 2;
                    line.MarkerSize = 6;
                    line.Color = Colors.Blue;

                    plot.Axes.AutoScale();
                    plot.Axes.Bottom.SetTicks(xs, labels);
                    plot.Axes.Bottom.MajorTickStyle.Length = 0;
                    if (config.IsRotate)
                    {

                        plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
                        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperRight;
                        plot.Axes.Bottom.TickLabelStyle.OffsetY = -11;
                        plot.Axes.Bottom.TickLabelStyle.OffsetX = 5;
                        plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
                    }

                    if (values.Min() < 0)
                    {
                        plot.Axes.Margins(bottom: .3, top: .3);
                    }
                    else
                    {
                        plot.Axes.Margins(bottom: 0, top: .3);
                    }
                    break;
                }

        }

        return plot.GetSvgXml(500, 300);
    }

    public static (string unit, double[] scaledNumbers) ScaleNumbers(double[] numbers)
    {
        if (numbers == null || numbers.Length == 0)
            return ("", new double[0]);

        double min = Math.Abs(numbers.Min());

        var scales = new List<(double threshold, string unit)>
            {
                (1_000_000_000_000_000, "کوادریلیون"),
                (1_000_000_000_000,     "تریلیون"),
                (1_000_000_000,         "میلیارد"),
                (1_000_000,             "میلیون"),
                (1_000,                 "هزار"),
                (1,                     "")
            };

        if (min < 10000) return (string.Empty, numbers);

        var selected = scales.First(s => min >= s.threshold);
        double scale = selected.threshold;
        string unit = selected.unit;

        var scaledNumbers = numbers.Select(x => x / scale).ToArray();

        return (unit, scaledNumbers);
    }
}

