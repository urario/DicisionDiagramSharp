using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp;

/// <summary>
/// Provides high-level text export helpers for MTBDD values.
/// </summary>
public static class MtbddExportExtensions
{
    /// <summary>
    /// Exports a bounded MTBDD value table as Markdown.
    /// </summary>
    public static string ToMarkdownValueTable(this Mtbdd value, TruthTableOptions? options = null)
    {
        return Export.MarkdownTableExporter.Export(value.ToValueTable(options));
    }

    /// <summary>
    /// Exports a bounded MTBDD value table as CSV.
    /// </summary>
    public static string ToCsvValueTable(this Mtbdd value, TruthTableOptions? options = null)
    {
        return Export.CsvTableExporter.Export(value.ToValueTable(options));
    }

    /// <summary>
    /// Exports a bounded MTBDD value table as AsciiDoc.
    /// </summary>
    public static string ToAsciiDocValueTable(this Mtbdd value, TruthTableOptions? options = null)
    {
        return Export.AsciiDocTableExporter.Export(value.ToValueTable(options));
    }
}
