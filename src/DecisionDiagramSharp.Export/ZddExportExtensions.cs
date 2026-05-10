using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp;

/// <summary>
/// Provides high-level text export helpers for ZDD values.
/// </summary>
public static class ZddExportExtensions
{
    /// <summary>
    /// Exports a bounded ZDD set-family table as Markdown.
    /// </summary>
    public static string ToMarkdownSetFamily(this Zdd value, SetEnumerationOptions? options = null)
    {
        return Export.MarkdownTableExporter.Export(value.ToSetFamilyTable(options));
    }

    /// <summary>
    /// Exports a bounded ZDD set-family table as CSV.
    /// </summary>
    public static string ToCsvSetFamily(this Zdd value, SetEnumerationOptions? options = null)
    {
        return Export.CsvTableExporter.Export(value.ToSetFamilyTable(options));
    }

    /// <summary>
    /// Exports a bounded ZDD set-family table as AsciiDoc.
    /// </summary>
    public static string ToAsciiDocSetFamily(this Zdd value, SetEnumerationOptions? options = null)
    {
        return Export.AsciiDocTableExporter.Export(value.ToSetFamilyTable(options));
    }
}
