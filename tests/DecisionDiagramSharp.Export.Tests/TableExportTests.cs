using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

namespace DecisionDiagramSharp.Export.Tests;

[TestClass]
public sealed class TableExportTests
{
    [TestMethod]
    public void CsvExporter_EscapesValues()
    {
        var table = CreateTable();
        var actual = Normalize(CsvTableExporter.Export(table));
        var expected = Normalize(
@"Name,Value
A,""x,y""
B,""line1
line2""
");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void MarkdownExporter_FormatsTable()
    {
        var table = CreateTable();
        var actual = Normalize(MarkdownTableExporter.Export(table));
        var expected = Normalize(
@"## Demo
| Name | Value |
| --- | --- |
| A | x,y |
| B | line1<br/>line2 |
");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void AsciiDocExporter_FormatsTable()
    {
        var table = CreateTable();
        var actual = Normalize(AsciiDocTableExporter.Export(table));
        var expected = Normalize(
@".Demo
|===
|Name |Value
|A |x,y
|B |line1 +
line2
|===
");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Exporters_FormatBddTruthTableModel()
    {
        // Purpose: Verify generic table exporters can format BDD diagnostics tables.
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");

        // Act
        var table = DecisionDiagramSharp.Diagnostics.BddDiagnostics.BuildTruthTable(manager, manager.Var(a));

        // Assert
        StringAssert.Contains(CsvTableExporter.Export(table), "A,Result");
        StringAssert.Contains(MarkdownTableExporter.Export(table), "| A | Result |");
        StringAssert.Contains(AsciiDocTableExporter.Export(table), "|A");
    }

    [TestMethod]
    public void Exporters_FormatMtbddValueTableModel()
    {
        // Purpose: Verify existing CSV, Markdown, and AsciiDoc exporters format MTBDD value-table diagnostics.
        // Arrange
        var manager = new MtbddManager();
        manager.GetOrAddVariable("A");
        var function = manager.Create(new[] { 8, -2 });

        // Act
        var table = DecisionDiagramSharp.Diagnostics.MtbddDiagnostics.BuildValueTable(manager, function);

        // Assert
        StringAssert.Contains(CsvTableExporter.Export(table), "A,Result");
        StringAssert.Contains(MarkdownTableExporter.Export(table), "| A | Result |");
        StringAssert.Contains(AsciiDocTableExporter.Export(table), "|8");
    }

    [TestMethod]
    public void Exporters_FormatZmtbddValueTableModel()
    {
        // Purpose: Verify existing CSV, Markdown, and AsciiDoc exporters format ZMTBDD value-table diagnostics.
        // Arrange
        var manager = new ZmtbddManager();
        manager.GetOrAddVariable("A");
        var function = manager.Create(new[] { 8, 0 });

        // Act
        var table = DecisionDiagramSharp.Diagnostics.ZmtbddDiagnostics.BuildValueTable(manager, function);

        // Assert
        StringAssert.Contains(CsvTableExporter.Export(table), "A,Result");
        StringAssert.Contains(MarkdownTableExporter.Export(table), "| A | Result |");
        StringAssert.Contains(AsciiDocTableExporter.Export(table), "|0");
    }

    private static TableModel CreateTable()
    {
        return new TableModel(
            "Demo",
            new[] { "Name", "Value" },
            new List<TableRow>
            {
                new TableRow(new[] { "A", "x,y" }),
                new TableRow(new[] { "B", "line1\nline2" })
            });
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
