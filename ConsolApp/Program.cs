using BluesReporter;
using BluesReporter.Models;
using ReportGenerator;

var report =new Report();
var staticReport =new StaticReport();

var template = TemplateConfig.Load(@"d:\\Template.json");
var rankingColor = new Dictionary<int, Func<int?, string>>();
rankingColor.Add(6, report.GetColor);
var builder = new ReportBuilder();
builder.GenerateStatic("",staticReport);
//builder.GenerateDynamic("", template, report,"Items", rankingColor);