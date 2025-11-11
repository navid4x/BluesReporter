using BluesReporter;
using BluesReporter.Charts;
using BluesReporter.Models;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections;
using System.Reflection;

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
                        col.Item().PaddingBottom(1).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < _config.Content.TotalColumns; i++)
                                    columns.RelativeColumn();
                            });

                            table.AddHeaderCells(_config.Content.Headers, _data);

                            table.AddDataCells(_config.Content.Data, _data, dataField, RankingMethod);
                        });
                        //col.Item().PreventPageBreak();
                        _config.Content.Charts = [new ChartConfig(), new ChartConfig()];
                        if (_config.Content.Charts.Any()) col.Item().AddDynamicChart(_config.Content.Charts, dataList!);

                    });

                    page.AddFooter(_config.Footer);
                });
            }).WithSettings(new DocumentSettings
            {
                PdfA=true,
                CompressDocument = true,
                ImageCompressionQuality = ImageCompressionQuality.High,
                ImageRasterDpi = 288,
                ContentDirection = ContentDirection.LeftToRight
            }).ShowInCompanion();
            return true;
        }


        private List<string> Validation<T>(TemplateConfig templateConfig, T model, string dataSet)
        {
            if (model is null)
            {
                Errors.Add("مدل وارد شده خالی می باشد");
                return Errors;
            }
            if (templateConfig.Content.TotalColumns != templateConfig.Content.Data.Count)
            {
                Errors.Add("تعداد کل ستون تعریف شده با مقداد TotalColumns برابر نمی باشد.");
            }

            List<string> fields = model.GetType().GetProperties().Select(a => a.Name).ToList();
            foreach (HeaderCellConfig data in templateConfig.Content.Headers)
            {
                if (!data.Text.StartsWith("{")) continue;

                string field = data.Text.Replace("{", "").Replace("}", "");
                if (!fields.Contains(field))
                {
                    Errors.Add($"فیلدی با نام {field} وجود ندارد.");
                }
            }


            System.Reflection.PropertyInfo? propInfo = typeof(TModel).GetProperty(dataSet);

            if (propInfo == null)
            {
                Errors.Add($"فیلدی با نام {dataSet} در مدل وجود ندارد.");
            }

            object? val = propInfo!.GetValue(model);


            List<object> list = val is System.Collections.IEnumerable items
                ? items.Cast<object>().ToList()
                : [];

            if (list.Count == 0)
            {
                Errors.Add($"رکوردی برای ساخت جدول وجود ندارد.");
            }


            List<string>? props = list.FirstOrDefault()?.GetType().GetProperties().Select(a => a.Name).ToList();
            foreach (DataCellConfig data in templateConfig.Content.Data)
            {
                if (data.Order == null)
                {
                    Errors.Add("وارد کردن مقدار order ضروری است.");
                }

                foreach (string rowSpan in data.RowSpan)
                {
                    if (!int.TryParse(rowSpan, out _) && !rowSpan.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        Errors.Add("مقدار وارد شده برای RowSpan نامعتبر است.");
                    }
                }

                string field = data.Field.Replace("{", "").Replace("}", "");
                if (!props!.Contains(field))
                {
                    Errors.Add($"فیلدی با نام {field} وجود ندارد.");
                }
            }

            return Errors;
        }
    }
}