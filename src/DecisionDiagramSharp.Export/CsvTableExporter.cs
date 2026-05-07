using System.Text;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Export;

/// <summary>
/// Exports <see cref="TableModel"/> values as CSV text.
/// </summary>
public static class CsvTableExporter
{
    /// <summary>
    /// Exports a table to CSV.
    /// </summary>
    public static string Export(TableModel table)
    {
        var sb = new StringBuilder();
        AppendRow(sb, table.Columns);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            AppendRow(sb, table.Rows[i].Cells);
        }

        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, System.Collections.Generic.IReadOnlyList<string> cells)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(Escape(cells[i] ?? string.Empty));
        }

        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        if (value.IndexOf('"') >= 0 || value.IndexOf(',') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
