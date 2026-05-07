using System.Text;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Export;

/// <summary>
/// Exports <see cref="TableModel"/> values as AsciiDoc tables.
/// </summary>
public static class AsciiDocTableExporter
{
    /// <summary>
    /// Exports a table to AsciiDoc.
    /// </summary>
    public static string Export(TableModel table)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(table.Title))
        {
            sb.AppendLine("." + table.Title);
        }

        sb.AppendLine("|===");
        AppendRow(sb, table.Columns);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            AppendRow(sb, table.Rows[i].Cells);
        }

        sb.AppendLine("|===");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, System.Collections.Generic.IReadOnlyList<string> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            sb.Append('|');
            sb.Append(Escape(cells[i] ?? string.Empty));
            if (i + 1 < cells.Count)
            {
                sb.Append(' ');
            }
        }

        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        return value.Replace("\r", " ").Replace("\n", " +\n");
    }
}
