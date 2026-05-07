using System;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

var manager = new ZddManager();
var a = manager.GetOrAddVariable("A");
var b = manager.GetOrAddVariable("B");
var c = manager.GetOrAddVariable("C");

var family = manager.MakeFamily(
    new[]
    {
        new[] { a, b },
        new[] { b, c },
        new[] { a, c }
    });

var setFamilyTable = ZddDiagnostics.BuildSetFamilyTable(manager, family);
var nodeTable = ZddDiagnostics.BuildNodeTable(manager, family);
var statsTable = ZddDiagnostics.BuildStatisticsTable(manager, family);
var dot = ZddDiagnostics.ToDot(manager, family);

Console.WriteLine("=== DOT ===");
Console.WriteLine(dot);
Console.WriteLine("=== CSV ===");
Console.WriteLine(CsvTableExporter.Export(setFamilyTable));
Console.WriteLine("=== Markdown ===");
Console.WriteLine(MarkdownTableExporter.Export(nodeTable));
Console.WriteLine("=== AsciiDoc ===");
Console.WriteLine(AsciiDocTableExporter.Export(statsTable));
