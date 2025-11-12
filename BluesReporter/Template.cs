using BluesReporter.Models;
using ScottPlot;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BluesReporter.Models
{
    public class TemplateConfig
    {
        public bool RTL { get; set; } = true;
        public float Margin { get; set; } = 10;
        public string Font { get; set; } = "B Nazanin";
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Landscape";

        public HeaderConfig Header { get; set; }
        public ContentConfig Content { get; set; } = new();
        public FooterConfig Footer { get; set; }

        public static TemplateConfig Load(string filePath)
        {
            try
            {

                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };
                return JsonSerializer.Deserialize<TemplateConfig>(json, options)!;
            }
            catch(Exception e)
            {
                throw new InvalidOperationException("فرمت قالب صحیح نمی باشد، لطفا قالب را بررسی نمایید.");
            }
        }

    }

    public class HeaderConfig
    {
        public string Text { get; set; } = string.Empty;
        public string Align { get; set; } = "center";
        public int FontSize { get; set; } = 20;
        public bool IsBold { get; set; } = true;
        public bool ShowOnce { get; set; } = true;
        public float PaddingBottom { get; set; }

    }

    public class FooterConfig
    {
        public string Align { get; set; } = "center";
        public float VerticalPadding { get; set; } = 10;
        public string SeperatorText { get; set; } = "/";
        public bool IsSimple { get; set; } = false;
    }

    public class ContentConfig
    {
        public int TotalColumns { get; set; }
        public float MarginBetween { get; set; } = 1;
        public List<HeaderCellConfig>? Headers { get; set; }
        public List<DataCellConfig>? Data { get; set; }
        public List<ChartConfig>? Charts { get; set; }
    }

    public class HeaderCellConfig
    {
        public uint ColSpan { get; set; } = 1;
        public string BGColor { get; set; } = "ffffff";
        public float BorderSize { get; set; } = 1;
        public float Padding { get; set; } = 3;
        public string Align { get; set; } = "center";
        public float FontSize { get; set; } = 16;
        public bool IsBold { get; set; } = true;
        public string Text { get; set; } = string.Empty;
        public uint RowSpan { get; set; } = 1;
    }

    public class DataCellConfig
    {
        public string BGColor { get; set; } = "ffffff";
        public float BorderSize { get; set; } = 1;
        public float Padding { get; set; } = 1;
        public string Align { get; set; } = "center";
        public string FormattingText { get; set; } = string.Empty;
        public bool RTL { get; set; } = true;
        public bool IsBold { get; set; } = false;
        public bool IsRepeated { get; set; } = true;
        public List<string> RowSpan { get; set; } = ["1"]; //use 'All' for all row
        public int FontSize { get; set; } = 14;
        public string Field { get; set; }
        public int? Order { get; set; }
        public bool RankingFlag { get; set; } = false;

    }
    public class ChartConfig
    {
        public int BorderSize { get; set; } = 1;
        public int Padding { get; set; } = 1;
        public string YValue { get; set; } 
        public string XValue { get; set; } 
        public string LegendItems { get; set; } 
        public string YLabel { get; set; }=string.Empty;
        public string XLabel { get; set; } = string.Empty;
        public Edge LegendAlign { get; set; } = Edge.Bottom;
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public ChartTypes ChartType { get; set; } = ChartTypes.Bar;
        public bool ShowLegend { get; set; } = true;
        public string Title { get; set; } = string.Empty;
        public string FontName { get; set; } = "P Nazanin";
        public int ValueFontSize { get; set; } = 14;
        public int LegendFontSize { get; set; } = 12;
        public int LabelFontSize { get; set; } = 16;
        public int TitleFontSize { get; set; } = 16;
        public int AxisFontSize { get; set; } = 14;
        public string FormattingText { get; set; } =string.Empty;
        public bool ShowValueLable { get; set; } =true;
        public bool IsRotate { get; set; } =true;
        public bool ShowXGrid { get; set; } = true;
        public bool ShowYGrid { get; set; } = true;

    }
    public enum ChartTypes
    {
        Pie,
        Line,
        Bar
    }
}

