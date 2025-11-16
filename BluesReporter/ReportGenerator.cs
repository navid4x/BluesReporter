using BluesReporter;
using BluesReporter.Charts;
using BluesReporter.Models;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static QuestPDF.Helpers.Colors;

namespace ReportGenerator
{
    public class ReportBuilder
    {
        public ReportBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseEnvironmentFonts = true;
        }
        public List<string> Errors { get; private set; } = [];

        public bool GenerateStatic(string outputPath, StaticReport data, bool IsTargetedUnit)
        {

            Unit unit = Unit.Millimetre;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(10, unit);
                    page.DefaultTextStyle(TextStyle.Default.FontFamily("P Nazanin"));

                    page.ContentFromRightToLeft();
                    page.Size(PageSizes.A4.Landscape());

                    page.Content().Column(col =>
                    {

                        col.Item().ScaleToFit().ShowEntire().Table(table =>
                        {

                            table.ColumnsDefinition(columns =>
                            {
                                int count = IsTargetedUnit ? 12 : 11;
                                for (int i = 0; i < count; i++)
                                    columns.RelativeColumn();
                            });

                            if (IsTargetedUnit)
                                table.Cell().RowSpan(9).Border(.5f).AlignMiddle().AlignCenter().Padding(5).Text(data!.TargetUnit).FontSize(16).Bold();

                            table.Cell().ColumnSpan(11).Background(Color.FromHex("#E6B8B7")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(data.IndicatorName).FontSize(16).Bold();

                            table.Cell().Background(Color.FromHex("#F2DCDB")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("رتبه").FontSize(14).Bold();

                            var changedRank = data.ValueList
                                                .GroupBy(s => s.BankName)
                                                .SelectMany(g =>
                                                    g.OrderBy(x => x.TargetDate)
                                                     .Select((x, i) => new
                                                     {
                                                         Change = i == 0 ? (int?)null : g.ElementAt(i - 1).Ranking - x.Ranking
                                                     })
                                                ).Where(a => a.Change is not null)
                                                .ToList();



                            var grouped = data.ValueList.GroupBy(s => s.TargetDate);

                            var show = true;
                            foreach (var date in grouped.First())
                            {
                                table.Cell().Border(.5f).Background(Color.FromHex("#F2DCDB")).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(date.BankName).FontSize(14).Bold();

                            }
                            table.Cell().ColumnSpan(4).Background(Color.FromHex("#F2DCDB")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("توضیحات").FontSize(14).Bold();

                            foreach (var date in grouped)
                            {
                                table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(date.Key).FontSize(14).Bold();

                                foreach (var unit in date)
                                {
                                    table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(unit.Ranking.ToString()).FontSize(14).Bold();
                                }
                                if (show)
                                {
                                    table.Cell().RowSpan(3).ColumnSpan(4).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(data.Description).FontSize(14).Bold();
                                    show = false;
                                }

                            }

                            table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("تغییرات").FontSize(14).Bold();

                            foreach (var data in changedRank)
                            {
                                table.Cell().Background(GetBackgroundColor(data.Change!.Value)).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(data.Change.ToString()).FontSize(14).Bold();


                            }


                            var changedValue = data.ValueList
                                             .GroupBy(s => s.BankName)
                                             .SelectMany(g =>
                                                 g.OrderBy(x => x.TargetDate)
                                                  .Select((x, i) => new
                                                  {
                                                      BankName = x.BankName,
                                                      Change = i == 0 ? null : (int?)(g.ElementAt(i - 1).Value - x.Value)
                                                  })
                                             ).Where(a => a.Change is not null)
                                             .ToList();

                            var list = changedValue.OrderByDescending(a => a.Change).ToList();
                            var tops = list.Take(2).ToList();
                            var downs = list.TakeLast(2).ToList();
                            var showtitle = true;
                            var showName = false;

                            table.Cell().Background(Color.FromHex("#F2DCDB")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("سهم از بازار").FontSize(14).Bold();

                            foreach (var date in grouped.First())
                            {
                                table.Cell().Background(Color.FromHex("#F2DCDB")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(date.BankName).FontSize(14).Bold();

                            }

                            table.Cell().ColumnSpan(4).Background(Color.FromHex("#F2DCDB")).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("توضیحات").FontSize(14).Bold();

                            foreach (var date in grouped)
                            {
                                table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(date.Key).FontSize(14).Bold();

                                foreach (var unit in date)
                                {
                                    table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(unit.Value.ToString()).FontSize(14).Bold();
                                }
                                if (showtitle)
                                {

                                    table.Cell().ColumnSpan(2).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("بیشترین افزایش سهم از بازار").FontSize(12).Bold().FontColor(Color.FromHex("#1515FF"));
                                    table.Cell().ColumnSpan(2).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("بیشترین کاهش سهم از بازار").FontSize(12).Bold().FontColor(Color.FromHex("#1515FF"));
                                    showtitle = false;
                                }
                                if (showName)
                                {

                                    foreach (var top in tops)
                                    {
                                        table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(top.BankName).FontSize(14).Bold().FontColor(Color.FromHex("#1515FF"));
                                    }
                                    foreach (var down in downs)
                                    {
                                        table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(down.BankName).FontSize(14).Bold().FontColor(Color.FromHex("#1515FF"));
                                    }

                                }
                                showName = true;


                            }

                            table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text("تغییرات").FontSize(14).Bold();

                            foreach (var data in changedValue)
                            {
                                table.Cell().Background(GetBackgroundColor(data.Change!.Value)).Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(data.Change.ToString()).FontSize(14).Bold();

                            }

                            foreach (var top in tops)
                            {
                                table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(top.Change.ToString()).FontSize(14).Bold().FontColor(GetTextColor(top.Change!.Value));
                            }
                            foreach (var down in downs)
                            {
                                table.Cell().Border(.5f).AlignCenter().AlignMiddle().Padding(5).PaddingVertical(8).Text(down.Change.ToString()).FontSize(14).Bold().FontColor(GetTextColor(down.Change!.Value));
                            }

                        });
                    });

                });
            }).WithSettings(new DocumentSettings
            {
                PdfA = true,
                CompressDocument = true,
                ImageCompressionQuality = ImageCompressionQuality.High,
                ImageRasterDpi = 288,
                ContentDirection = ContentDirection.RightToLeft,
            }).ShowInCompanion();
            return true;
        }
        public bool GenerateDynamic(string outputPath, TemplateConfig _config, object _data, string dataField, Dictionary<int, Func<int?, string>>? RankingMethod = default)
        {
            var errors = Validation(_config, _data, dataField);
            if (errors.Count != 0) return false;

            var dataList = _data.GetType().GetProperty(dataField)!.GetValue(_data)!;

            Unit unit = Unit.Millimetre;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(_config.Margin, unit);
                    page.DefaultTextStyle(TextStyle.Default.FontFamily(_config.Font));

                    if (_config.RTL)
                        page.ContentFromRightToLeft();
                    page.PageSize(_config.PaperSize, _config.Orientation);

                    page.AddPageHeader(_config.Header);

                    page.Content().Column(col =>
                    {
                        if (_config.Content.Data != null && _config.Content.Headers != null)
                        {

                            col.Item().PaddingBottom(_config.Content.MarginBetween).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    for (int i = 0; i < _config.Content.TotalColumns; i++)
                                        columns.RelativeColumn();
                                });

                                table.AddHeaderCells(_config.Content.Headers, _data);

                                table.AddDataCells(_config.Content.Data, _data, dataField, RankingMethod);
                            });
                        }

                        if (_config.Content.Charts != null && _config.Content.Charts.Any()) col.Item().PaddingBottom(_config.Content.MarginBetween).AddDynamicChart(_config.Content.Charts, dataList!);

                    });

                    page.AddFooter(_config.Footer);
                });
            }).WithSettings(new DocumentSettings
            {
                PdfA = true,
                CompressDocument = true,
                ImageCompressionQuality = ImageCompressionQuality.High,
                ImageRasterDpi = 288,
                ContentDirection = ContentDirection.RightToLeft,
            }).ShowInCompanion();
            return true;

        }
        private void BuildCell(TableDescriptor table)
        {

        }
        private Color GetBackgroundColor(int number)
        {
            if (number == 0) return Color.FromHex("#DCE6F1");
            if (number > 0) return Color.FromHex("#92D050");
            if (number < 0) return Color.FromHex("#FF8989");
            return Blue.Lighten5;
        }
        private Color GetTextColor(int number)
        {
            if (number == 0) return Color.FromHex("#000000");
            if (number > 0) return Color.FromHex("#00B050");
            if (number < 0) return Color.FromHex("#FF0000");
            return Blue.Lighten5;
        }

        #region Validations

        private List<string> Validation<T>(TemplateConfig templateConfig, T model, string dataSet)
        {
            if (model == null)
            {
                Errors.Add("مدل وارد شده خالی می باشد");
                return Errors;
            }

            var modelProps = model.GetType().GetProperties().Select(prop => prop.Name).ToHashSet();

            var dataSource = model.GetType().GetProperty(dataSet)?.GetValue(model);
            if (dataSource == null)
            {
                Errors.Add($"فیلدی با نام {dataSet} در مدل وجود ندارد.");
                return Errors;
            }

            var list = dataSource is IEnumerable<object> items ? items.ToList() : new List<object>();
            if (!list.Any())
            {
                Errors.Add("رکوردی برای ساخت جدول وجود ندارد.");
                return Errors;
            }

            var dataSetProps = list.First().GetType().GetProperties().Select(prop => prop.Name).ToHashSet();

            ValidateTemplateConfig(templateConfig, modelProps, dataSetProps);
            return Errors;
        }

        private void ValidateTemplateConfig(TemplateConfig templateConfig, HashSet<string> modelProps, HashSet<string> dataSetProps)
        {
            if (templateConfig.Content.Data != null && templateConfig.Content.Headers != null)
            {
                if (templateConfig.Content.TotalColumns != templateConfig.Content.Data.Count)
                {
                    Errors.Add("تعداد کل ستون تعریف شده با مقداد TotalColumns برابر نمی باشد.");
                }

                ValidateHeaders(templateConfig.Content.Headers, modelProps);
                ValidateDataCells(templateConfig.Content.Data, dataSetProps);

            }

            if (templateConfig.Content.Charts != null)
            {
                ValidateCharts(templateConfig.Content.Charts, dataSetProps);
            }
        }

        private void ValidateHeaders(IEnumerable<HeaderCellConfig> headers, HashSet<string> fields)
        {

            foreach (var header in headers)
            {
                if (header.Text.StartsWith("{"))
                {
                    var field = header.Text.Trim('{', '}');
                    if (!fields.Contains(field))
                    {
                        Errors.Add($"فیلدی با نام {field} وجود ندارد.");
                    }
                }
            }

        }

        private void ValidateDataCells(IEnumerable<DataCellConfig> dataCells, HashSet<string> props)
        {
            foreach (var data in dataCells)
            {
                if (data.Order == null)
                {
                    Errors.Add("وارد کردن مقدار order ضروری است.");
                }

                foreach (var rowSpan in data.RowSpan)
                {
                    if (!int.TryParse(rowSpan, out _) && !rowSpan.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        Errors.Add("مقدار وارد شده برای RowSpan نامعتبر است.");
                    }
                }

                if (data.Field.StartsWith('{'))
                {
                    var field = data.Field.Trim('{', '}');
                    if (!props.Contains(field))
                    {
                        Errors.Add($"فیلدی با نام {field} وجود ندارد.");
                    }
                }
            }
        }

        private void ValidateCharts(IEnumerable<ChartConfig> charts, HashSet<string> props)
        {

            foreach (var chart in charts)
            {
                if (chart.ShowLegend)
                {
                    if (string.IsNullOrEmpty(chart.LegendItems))
                        Errors.Add("فیلدی برای راهنما تعریف نشده است");
                    else
                        ValidateChartField(chart.LegendItems, props);

                }
                ValidateChartField(chart.XValue, props);
                ValidateChartField(chart.YValue, props);
            }
        }

        private void ValidateChartField(string field, HashSet<string> props)
        {

            if (field.StartsWith("{"))
            {
                var trimmedField = field.Trim('{', '}');
                if (!props.Contains(trimmedField))
                {
                    Errors.Add($"فیلدی با نام {trimmedField} وجود ندارد.");
                }
            }

        }

        #endregion
    }
}