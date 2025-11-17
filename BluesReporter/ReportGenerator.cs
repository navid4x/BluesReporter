using BluesReporter;
using BluesReporter.Charts;
using BluesReporter.Models;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot.Colormaps;
using static QuestPDF.Helpers.Colors;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

                                    if (IsTargetedUnit && i < 2)
                                        columns.RelativeColumn(1.3f);
                                    else if (!IsTargetedUnit && i == 0)
                                        columns.RelativeColumn(1.3f);
                                    else
                                        columns.RelativeColumn();


                            });

                            if (IsTargetedUnit)
                                BuildCell(table, data!.TargetUnit, rowSpan: 11);

                            BuildCell(table, data.IndicatorName, "#E6B8B7", fontSize: 16, colSpan: 11);

                            BuildCell(table, "رتبه", "#F2DCDB");


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
                                BuildCell(table, date.BankName, "#F2DCDB");
                            }
                            BuildCell(table, "توضیحات", "#F2DCDB", colSpan: 4);

                            foreach (var date in grouped)
                            {
                                BuildCell(table, date.Key);

                                foreach (var unit in date)
                                {
                                    BuildCell(table, unit.Ranking.ToString());
                                }
                                if (show)
                                {
                                    BuildCell(table, data.Description, colSpan: 4, rowSpan: 3);
                                    show = false;
                                }

                            }

                            BuildCell(table, "تغییرات");

                            foreach (var data in changedRank)
                            {
                                BuildCell(table, data.Change.ToString()!, GetBackgroundColor(data.Change!.Value),IsRTL:false);

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

                            BuildCell(table, "سهم از بازار", "#F2DCDB");

                            foreach (var date in grouped.First())
                            {
                                BuildCell(table, date.BankName, "#F2DCDB");
                            }

                            BuildCell(table, "توضیحات", "#F2DCDB", colSpan: 4);

                            foreach (var date in grouped)
                            {
                                BuildCell(table, date.Key);

                                foreach (var unit in date)
                                {
                                    BuildCell(table, unit.Value.ToString());
                                }
                                if (showtitle)
                                {
                                    BuildCell(table, "بیشترین افزایش سهم", colSpan: 2, textColor: "#1515FF", fontSize: 12);
                                    BuildCell(table, "بیشترین کاهش سهم", colSpan: 2, textColor: "#1515FF", fontSize: 12);

                                    showtitle = false;
                                }
                                if (showName)
                                {

                                    foreach (var top in tops)
                                    {
                                        BuildCell(table, top.BankName, textColor: "#1515FF");
                                    }
                                    foreach (var down in downs)
                                    {
                                        BuildCell(table, down.BankName, textColor: "#1515FF");
                                    }

                                }
                                showName = true;


                            }

                            BuildCell(table, "تغییرات");

                            foreach (var data in changedValue)
                            {
                                BuildCell(table, data.Change.ToString()!, backgroundColor: GetBackgroundColor(data.Change!.Value), IsRTL: false);
                            }

                            foreach (var top in tops)
                            {
                                BuildCell(table, top.Change.ToString()!, textColor: GetTextColor(top.Change!.Value), IsRTL: false);
                            }
                            foreach (var down in downs)
                            {
                                BuildCell(table, down.Change.ToString()!, textColor: GetTextColor(down.Change!.Value), IsRTL: false);
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
            }).GeneratePdf(outputPath);
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
            }).GeneratePdf(outputPath);
            return true;

        }
        private void BuildCell(TableDescriptor table, string data, string backgroundColor = "ffffff", string textColor = "000000", int fontSize = 14, uint colSpan = 1, uint rowSpan = 1,bool IsRTL=true)
        {
            table.Cell()
                .ColumnSpan(colSpan)
                .RowSpan(rowSpan)
                .Background(Color.FromHex(backgroundColor))
                .Border(.5f)
                .AlignCenter()
                .AlignMiddle()
                .PaddingHorizontal(5)
                .PaddingVertical(12)
                .NotRTL(IsRTL)
                .Text(data)
                .FontSize(fontSize)
                .Bold()
                .FontColor(Color.FromHex(textColor));

        }
        private string GetBackgroundColor(int number)
        {
            if (number == 0) return "#DCE6F1";
            if (number > 0) return "#92D050";
            if (number < 0) return "#FF8989";
            return "#DCE6F1";
        }
        private string GetTextColor(int number)
        {
            if (number == 0) return "#000000";
            if (number > 0) return "#00B050";
            if (number < 0) return "#FF0000";
            return "#000000";
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