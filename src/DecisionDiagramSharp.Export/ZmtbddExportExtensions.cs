using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp;

/// <summary>
/// Provides high-level text export helpers for ZMTBDD values.
/// </summary>
public static class ZmtbddExportExtensions
{
    /// <summary>
    /// Exports a bounded ZMTBDD value table as Markdown.
    /// </summary>
    public static string ToMarkdownValueTable(this Zmtbdd value, TruthTableOptions? options = null)
    {
        return Export.MarkdownTableExporter.Export(value.ToValueTable(options));
    }

    /// <summary>
    /// Exports a bounded ZMTBDD value table as CSV.
    /// </summary>
    public static string ToCsvValueTable(this Zmtbdd value, TruthTableOptions? options = null)
    {
        return Export.CsvTableExporter.Export(value.ToValueTable(options));
    }

    /// <summary>
    /// Exports a bounded ZMTBDD value table as AsciiDoc.
    /// </summary>
    public static string ToAsciiDocValueTable(this Zmtbdd value, TruthTableOptions? options = null)
    {
        return Export.AsciiDocTableExporter.Export(value.ToValueTable(options));
    }
}
