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
