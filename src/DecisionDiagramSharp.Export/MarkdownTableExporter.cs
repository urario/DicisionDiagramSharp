using System.Text;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Export;

/// <summary>
/// Exports <see cref="TableModel"/> values as Markdown tables.
/// </summary>
public static class MarkdownTableExporter
{
    /// <summary>
    /// Exports a table to Markdown.
    /// </summary>
    public static string Export(TableModel table)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(table.Title))
        {
            sb.AppendLine("## " + table.Title);
        }

        AppendPipeRow(sb, table.Columns);
        AppendSeparator(sb, table.Columns.Count);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            AppendPipeRow(sb, table.Rows[i].Cells);
        }

        return sb.ToString();
    }

    private static void AppendPipeRow(StringBuilder sb, System.Collections.Generic.IReadOnlyList<string> cells)
    {
        sb.Append('|');
        for (var i = 0; i < cells.Count; i++)
        {
            sb.Append(' ');
            sb.Append(Escape(cells[i] ?? string.Empty));
            sb.Append(" |");
        }

        sb.AppendLine();
    }

    private static void AppendSeparator(StringBuilder sb, int count)
    {
        sb.Append('|');
        for (var i = 0; i < count; i++)
        {
            sb.Append(" --- |");
        }

        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", "<br/>");
    }
}
