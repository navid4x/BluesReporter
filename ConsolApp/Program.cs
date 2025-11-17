using BluesReporter.Models;
using BluesReporter;
using ReportGenerator;
using System.Diagnostics;

Stopwatch sw = Stopwatch.StartNew();

var report = new Report();
var staticReport = new StaticReport();
var rankingColor = new Dictionary<int, Func<int?, string>>();
rankingColor.Add(6, report.GetColor);

sw.Start();
var template = TemplateConfig.Load(@"d:\\Template.json");
var builder = new ReportBuilder();
for (int i = 0; i < 1000; i++)
{
    builder.GenerateDynamic($"d:\\Text\\{i}.pdf", template, report, "Items", rankingColor);

}
sw.Stop();

var a = sw.ElapsedMilliseconds;

builder.GenerateStatic($"d:\\Text\\.pdf", staticReport, true);
