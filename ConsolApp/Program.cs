using BluesReporter;
using BluesReporter.Models;
using ReportGenerator;

var report =new Report();

var template = TemplateConfig.Load(@"d:\\Template.json");
var rankingColor = new Dictionary<int, Func<int?, string>>();
rankingColor.Add(6, report.GetColor);
var builder = new ReportBuilder<Report>(template, report, "Items", rankingColor);
builder.Generate(@"d:\\Template.pdf");