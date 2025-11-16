using QuestPDF.Infrastructure;
using static QuestPDF.Helpers.Colors;

namespace BluesReporter
{
    public class ReportItem
    {
        public decimal SourceValue { get; set; }
        public decimal DestinationValue { get; set; }
        public decimal ChangeValue => DestinationValue - SourceValue;
        public float GrowthValue => (float)((DestinationValue - SourceValue) / SourceValue * 100);
        public float TotalGrowthValue { get; set; }
        public string UnitName { get; set; }
        public int? Ranking { get; set; }

    }
    public class Report
    {
        public int InidcatorId { get; set; } = 1;
        public string IndicatorName { get; set; } = "مانده سود سپرده قرض الحسنه";
        public string SourceDate { get; set; } = "شهریور 1404";
        public string DestinationDate { get; set; } = "اسفند 1404";

        public List<ReportItem> Items { get; set; } = new();

        public string GetColor(int? rank)
        {
            var color = rank switch
            {
                >= 7 => "#FF1C1C",
                >= 4 => "#FFCC1D",
                <= 3 => "#11C902",
                _ => "#ffffff"
            };
            return color;
        }

        public Report()
        {

            List<string> Units = ["منطقه یک تهران", "منطقه دو تهران", "منطقه سه تهران", "منطقه چهار تهران", "منطقه پنج تهران", "استان البرز", "مدیریت شعبه ظفر"];
            Random rnd = new Random();



            foreach (var unit in Units)
            {
                Items.Add(new ReportItem()
                {
                    UnitName = unit,
                    SourceValue = rnd.Next(52000000, 130000000),
                    DestinationValue = rnd.Next(50000000, 130000000),
                });

            }

            int rank = 1;
            Items = Items.OrderByDescending(a => a.GrowthValue).ToList();
            Items.ForEach(a => a.Ranking = rank++);
            Items.Add(new ReportItem()
            {
                UnitName = "ناحیه یک",
                SourceValue = Items.Sum(a => a.SourceValue),
                DestinationValue = Items.Sum(a => a.DestinationValue),
            });
            Items.Add(new ReportItem()
            {
                UnitName = "کل بانک",
                SourceValue = 2095214576,
                DestinationValue = 3011254976,

            });
            var a = Items.FirstOrDefault(a => a.UnitName.Contains("کل بانک"));
            a.TotalGrowthValue = (float)(((a.DestinationValue - a.SourceValue) / a.SourceValue) * 100);

        }
    }

    public class StaticReport
    {
        public string TargetUnit { get; set; }
        public string IndicatorName { get; set; }
        public string Description { get; set; }=string.Empty;
        public List<StaticReportItem> ValueList { get; set; } = new();


        public StaticReport()
        {
            List<string> Banks = ["تجارت", "ملت", "صادرات", "رفاه", "سپه", "ملی"];
            Random rnd = new Random();
            List<StaticReportItem> list= new();
            TargetUnit = "البرز";
            IndicatorName = "وضعیت مانده سپرده";
            Description= string.Empty;

            foreach (var bank in Banks)
            {
                list.Add(new StaticReportItem()
                {
                    BankName = bank,
                    Value = rnd.Next(10, 30),
                    TargetDate = "اسفند 1402",
                });

            }
            int i = 1;
            list.OrderBy(a => a.Value).ToList().ForEach(a=>a.Ranking=i++);
            ValueList.AddRange(list);

            list.Clear();
            foreach (var bank in Banks)
            {
                list.Add(new StaticReportItem()
                {
                    BankName = bank,
                    Value = rnd.Next(10, 30),
                    TargetDate = "اسفند 1403",
                });

            }
            i = 1;
            list.OrderBy(a => a.Value).ToList().ForEach(a => a.Ranking = i++);
            ValueList.AddRange(list);


        }
    }
    public class StaticReportItem
    {
        public string BankName { get; set; }
        public double Value { get; set; }
        public string TargetDate { get; set; }
        public int Ranking { get; set; }

    }
}
