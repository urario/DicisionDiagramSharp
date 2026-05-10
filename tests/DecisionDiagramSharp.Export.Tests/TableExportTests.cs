using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

namespace DecisionDiagramSharp.Export.Tests;

[TestClass]
public sealed class TableExportTests
{
    /// <summary>
    /// Verifies that CsvTableExporter escapes commas and newlines in cell values.
    /// </summary>
    /// <remarks>
    /// Confirms RFC 4180 CSV escaping rules are applied so consumers can parse the output reliably.
    /// </remarks>
    [TestMethod]
    public void CsvExporter_EscapesValues()
    {
        // Arrange
        var table = CreateTable();

        // Act
        var actual = Normalize(CsvTableExporter.Export(table));
        var expected = Normalize(
@"Name,Value
A,""x,y""
B,""line1
line2""
");

        // Assert
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that MarkdownTableExporter produces correctly aligned Markdown table output.
    /// </summary>
    /// <remarks>
    /// Confirms Markdown table syntax (header, separator, rows, inline newlines as &lt;br/&gt;)
    /// so consumers can embed the output in documentation.
    /// </remarks>
    [TestMethod]
    public void MarkdownExporter_FormatsTable()
    {
        // Arrange
        var table = CreateTable();

        // Act
        var actual = Normalize(MarkdownTableExporter.Export(table));
        var expected = Normalize(
@"## Demo
| Name | Value |
| --- | --- |
| A | x,y |
| B | line1<br/>line2 |
");

        // Assert
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that AsciiDocTableExporter produces correctly structured AsciiDoc table output.
    /// </summary>
    /// <remarks>
    /// Confirms AsciiDoc table block syntax (table title, header, rows) so consumers can embed
    /// the output in AsciiDoc documentation.
    /// </remarks>
    [TestMethod]
    public void AsciiDocExporter_FormatsTable()
    {
        // Arrange
        var table = CreateTable();

        // Act
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

        // Assert
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that all three exporters can format a BDD truth table produced by BddDiagnostics.
    /// </summary>
    /// <remarks>
    /// Confirms that the generic table exporters integrate correctly with BDD diagnostics output.
    /// </remarks>
    [TestMethod]
    public void Exporters_FormatBddTruthTableModel()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");

        // Act
        var table = BddDiagnostics.BuildTruthTable(manager, manager.Var(a));

        // Assert
        StringAssert.Contains(CsvTableExporter.Export(table), "A,Result");
        StringAssert.Contains(MarkdownTableExporter.Export(table), "| A | Result |");
        StringAssert.Contains(AsciiDocTableExporter.Export(table), "|A");
    }

    /// <summary>
    /// Verifies that all three exporters can format an MTBDD value table produced by MtbddDiagnostics.
    /// </summary>
    /// <remarks>
    /// Confirms that the generic table exporters integrate correctly with MTBDD diagnostics output.
    /// </remarks>
    [TestMethod]
    public void Exporters_FormatMtbddValueTableModel()
    {
        // Arrange
        var manager = new MtbddManager();
        manager.GetOrAddVariable("A");
        var function = manager.Create(new[] { 8, -2 });

        // Act
        var table = MtbddDiagnostics.BuildValueTable(manager, function);

        // Assert
        StringAssert.Contains(CsvTableExporter.Export(table), "A,Result");
        StringAssert.Contains(MarkdownTableExporter.Export(table), "| A | Result |");
        StringAssert.Contains(AsciiDocTableExporter.Export(table), "|8");
    }

    /// <summary>
    /// Verifies that all three exporters can format a ZMTBDD value table produced by ZmtbddDiagnostics.
    /// </summary>
    /// <remarks>
    /// Confirms that the generic table exporters integrate correctly with ZMTBDD diagnostics output.
    /// </remarks>
    [TestMethod]
    public void Exporters_FormatZmtbddValueTableModel()
    {
        // Arrange
        var manager = new ZmtbddManager();
        manager.GetOrAddVariable("A");
        var function = manager.Create(new[] { 8, 0 });

        // Act
        var table = ZmtbddDiagnostics.BuildValueTable(manager, function);

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
