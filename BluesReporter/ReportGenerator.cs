using BluesReporter;
using BluesReporter.Charts;
using BluesReporter.Models;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ScottPlot.TickGenerators.TimeUnits;
using System.Collections;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReportGenerator
{
    public class ReportBuilder<TModel>(TemplateConfig _config, TModel _data, string dataField, Dictionary<int, Func<int?, string>>? RankingMethod = default) where TModel : class
    {
        public List<string> Errors { get; private set; } = [];

        public bool Generate(string outputPath)
        {

            List<string> errors = Validation(_config, _data, dataField);
            if (errors.Count != 0) return false;

            var dataList = _data.GetType().GetProperty(dataField)!.GetValue(_data);

            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseEnvironmentFonts = true;
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
    }
}