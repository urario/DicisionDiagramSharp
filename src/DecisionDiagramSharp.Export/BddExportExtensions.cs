using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp;

/// <summary>
/// Provides high-level text export helpers for BDD values.
/// </summary>
public static class BddExportExtensions
{
    /// <summary>
    /// Exports a bounded BDD truth table as Markdown.
    /// </summary>
    public static string ToMarkdownTruthTable(this Bdd value, TruthTableOptions? options = null)
    {
        return Export.MarkdownTableExporter.Export(value.ToTruthTable(options));
    }

    /// <summary>
    /// Exports a bounded BDD truth table as CSV.
    /// </summary>
    public static string ToCsvTruthTable(this Bdd value, TruthTableOptions? options = null)
    {
        return Export.CsvTableExporter.Export(value.ToTruthTable(options));
    }

    /// <summary>
    /// Exports a bounded BDD truth table as AsciiDoc.
    /// </summary>
    public static string ToAsciiDocTruthTable(this Bdd value, TruthTableOptions? options = null)
    {
        return Export.AsciiDocTableExporter.Export(value.ToTruthTable(options));
    }

    /// <summary>
    /// Exports a bounded BDD satisfying-model table as Markdown.
    /// </summary>
    public static string ToMarkdownModels(this Bdd value, ModelEnumerationOptions? options = null)
    {
        return Export.MarkdownTableExporter.Export(value.ToModelTable(options));
    }

    /// <summary>
    /// Exports a bounded BDD satisfying-model table as CSV.
    /// </summary>
    public static string ToCsvModels(this Bdd value, ModelEnumerationOptions? options = null)
    {
        return Export.CsvTableExporter.Export(value.ToModelTable(options));
    }

    /// <summary>
    /// Exports a bounded BDD satisfying-model table as AsciiDoc.
    /// </summary>
    public static string ToAsciiDocModels(this Bdd value, ModelEnumerationOptions? options = null)
    {
        return Export.AsciiDocTableExporter.Export(value.ToModelTable(options));
    }
}
