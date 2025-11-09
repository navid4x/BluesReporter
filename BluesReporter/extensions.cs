using BluesReporter.Models;
using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections;

namespace BluesReporter
{
    public static class PageDescriptorExtensions
    {
        private static readonly AsyncLocal<Dictionary<int, RowSpanState>> RowSpanTracker = new();
        private static Unit unit = Unit.Millimetre;

        public static void PageSize(this PageDescriptor pageDescriptor, string size, string orientation)
        {
            PageSize pageSize = size.ToUpper() switch
            {
                "A0" => PageSizes.A0,
                "A1" => PageSizes.A1,
                "A2" => PageSizes.A2,
                "A3" => PageSizes.A3,
                "A4" => PageSizes.A4,
                "A5" => PageSizes.A5,
                "A6" => PageSizes.A6,
                _ => PageSizes.A4
            };
            PageSize Size = orientation.ToUpper() switch
            {

                "PORTRAIT" => pageSize.Portrait(),
                "LANDSCAPE" => pageSize.Landscape(),
                _ => pageSize.Landscape()
            };


            pageDescriptor.Size(Size);
        }
        public static PageDescriptor AddPageHeader(this PageDescriptor pageDescriptor, HeaderConfig? headerConfig)
        {
            if (headerConfig == null) return pageDescriptor;

            pageDescriptor.Header()
                 .ShowOncee(headerConfig.ShowOnce)
                 .PaddingBottom(headerConfig.PaddingBottom, unit)
                 .HAlign(headerConfig.Align)
                 .Text(headerConfig.Text)
                 .IsBold(headerConfig.IsBold)
                 .FontSize(headerConfig.FontSize);

            return pageDescriptor;
        }
        public static IContainer ShowOncee(this IContainer container, bool confirm = false)
        {
            return confirm ? container.ShowOnce() : container;
        }
        public static IContainer IsRepeated(this IContainer container, bool confirm = false)
        {
            return confirm ? container.Repeat() : container;
        }
        public static IContainer HAlign(this IContainer container, string align)
        {
            return align.ToLower() switch
            {
                "left" => container.AlignLeft().AlignMiddle(),
                "right" => container.AlignRight().AlignMiddle(),
                "center" => container.AlignCenter().AlignMiddle(),
                _ => container.AlignCenter().AlignMiddle(),
            };
        }
        public static TextBlockDescriptor IsBold(this TextBlockDescriptor descriptor, bool confirm = false)
        {
            if (confirm) descriptor.Bold();
            return descriptor;

        }
        public static IContainer NotRTL(this IContainer descriptor, bool confirm = true)
        {
            return !confirm ? descriptor.ContentFromLeftToRight() : descriptor;

        }
        public static TableDescriptor AddHeaderCells<T>(this TableDescriptor descriptor, List<HeaderCellConfig>? headerCells, T? model = default) where T : class
        {

            if (headerCells is null || !headerCells.Any()) return descriptor;

            descriptor.Header(header =>
            {
                foreach (HeaderCellConfig item in headerCells)
                {
                    string? data = string.Empty;
                    if (item.Text.StartsWith('{') && model is not null)
                    {
                        var query = item.Text.Replace("{", "").Replace("}", "");
                        var prob = model.GetType().GetProperties().FirstOrDefault(a => a.Name == query);
                        data = prob?.GetValue(model)?.ToString() ?? "-";
                    }
                    else
                    {
                        data = item.Text;
                    }

                    header.Cell().ColumnSpan(item.ColSpan)
                                     .RowSpan(item.RowSpan)
                                     .Background(Color.FromHex(item.BGColor))
                                     .Border(item.BorderSize)
                                     .Padding(item.Padding)
                                     .HAlign(item.Align)
                                     .Text(data)
                                     .IsBold(item.IsBold)
                                     .FontSize(item.FontSize);
                }
            });

            return descriptor;
        }

        public static TableDescriptor AddDataCells<TModel>(
            this TableDescriptor descriptor,
            List<DataCellConfig> configs,
            TModel model,
            string dataField,
            Dictionary<int, Func<int?, string>>? rankingMethods = null)
            where TModel : class
        {
            if (model == null || configs == null || configs.Count == 0 || string.IsNullOrWhiteSpace(dataField))
                return descriptor;

            var dataProp = model.GetType().GetProperty(dataField);
            if (dataProp == null) return descriptor;

            var values = dataProp.GetValue(model) as IList;
            if (values == null || values.Count == 0) return descriptor;

            var itemType = values[0]!.GetType();
            var totalRows = values.Count;

            var propCache = configs.ToDictionary(
                c => c.Field,
                c => itemType.GetProperty(c.Field));

            var sortedConfigs = configs.OrderBy(c => c.Order).ToList();

            if (RowSpanTracker.Value == null)
                RowSpanTracker.Value = new();

            var tracker = RowSpanTracker.Value;
            tracker.Clear();

            for (int i = 0; i < sortedConfigs.Count; i++)
            {
                tracker[i] = new RowSpanState
                {
                    Instructions = sortedConfigs[i].RowSpan?.ToList()!
                };
            }

            for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
            {
                var item = values[rowIndex];

                for (int colIndex = 0; colIndex < sortedConfigs.Count; colIndex++)
                {
                    var state = tracker[colIndex];

                    if (state.Remaining > 0)
                    {
                        state.Remaining--;
                        continue;
                    }

                    var config = sortedConfigs[colIndex];
                    var prop = propCache[config.Field];
                    var rawValue = prop.GetValue(item) ?? "";
                    var cellText = FormatCellValue(rawValue, config.FormattingText);

                    var bgColor = GetBackgroundColor(cellText, config, colIndex, rankingMethods);

                    var fontColor = GetTextColor(rawValue.ToString()!);

                    int rowSpan = CalculateCurrentRowSpan(state, totalRows, rowIndex);

                    var cell = descriptor.Cell();
                    if (rowSpan > 1)
                        cell.RowSpan((uint)rowSpan);

                    ApplyCellSettings(cell, config, bgColor, fontColor, cellText);

                    if (rowSpan > 1)
                    {
                        state.Remaining = rowSpan - 1;
                        if (state.Instructions.Count > 0)
                            state.Instructions.RemoveAt(0);
                    }
                }
            }

            return descriptor;
        }

        private static Color GetBackgroundColor(
            string text,
            DataCellConfig config,
            int columnIndex,
            Dictionary<int, Func<int?, string>>? rankingMethods)
        {
            if (config.RankingFlag && rankingMethods != null && rankingMethods.TryGetValue(columnIndex, out var rankingMethod))
            {
                if (int.TryParse(text, out int rank))
                {
                    var hex = rankingMethod(rank);
                    if (!string.IsNullOrEmpty(hex))
                        return Color.FromHex(hex);
                }
            }

            return Color.FromHex(config.BGColor);
        }

        private static Color GetTextColor(string data)
        {
            if (decimal.TryParse(data, out var number) && number < 0) return Colors.Red.Accent3;

            return Colors.Black;
        }

        private static void ApplyCellSettings(ITableCellContainer cell, DataCellConfig c, Color bg, Color textColor, string text)
        {
            cell.Background(bg)
                .Border(c.BorderSize)
                .Padding(c.Padding)
                .HAlign(c.Align)
                .IsRepeated(c.IsRepeated)
                .NotRTL(c.RTL)
                .Text(text)
                .FontColor(textColor)
                .IsBold(c.IsBold)
                .FontSize(c.FontSize);
        }

        private static int CalculateCurrentRowSpan(RowSpanState state, int totalRows, int currentRow)
        {
            if (state.Instructions.Count == 0) return 1;

            var instruction = state.Instructions[0].Trim();

            if (int.TryParse(instruction, out int num) && num > 0)
                return Math.Min(num, totalRows - currentRow);

            if (instruction.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                int usedByLater = 0;
                for (int i = 1; i < state.Instructions.Count; i++)
                {
                    if (int.TryParse(state.Instructions[i], out int laterNum))
                        usedByLater += laterNum;
                }

                int remaining = totalRows - currentRow;
                return Math.Max(1, remaining - usedByLater);
            }

            return 1;
        }

        private static string FormatCellValue(object value, string? format)
        {
            if (value == null) return "-";
            if (!string.IsNullOrEmpty(format) && decimal.TryParse(value.ToString(), out var num))
                return num.ToString(format);
            return value.ToString() ?? "-";
        }

        private class RowSpanState
        {
            public List<string> Instructions { get; set; } = new();
            public int Remaining { get; set; } = 0;
        }

        public static PageDescriptor AddFooter(this PageDescriptor pageDescriptor, FooterConfig? footerConfig)
        {
            if (footerConfig == null) return pageDescriptor;

            pageDescriptor.Footer()
                          .PaddingVertical(footerConfig.VerticalPadding)
                          .HAlign(footerConfig.Align)
                          .Text(text =>
                          {
                              if (footerConfig.IsSimple)
                              {
                                  text.CurrentPageNumber();

                              }
                              else
                              {
                                  text.CurrentPageNumber();
                                  text.Span(footerConfig.SeperatorText);
                                  text.TotalPages();
                              }
                          });
            return pageDescriptor;

        }
    }
}
