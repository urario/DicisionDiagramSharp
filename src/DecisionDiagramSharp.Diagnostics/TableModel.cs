using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Generic immutable table model used by text exporters.
/// </summary>
public sealed class TableModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableModel"/> class.
    /// </summary>
    public TableModel(string title, IReadOnlyList<string> columns, IReadOnlyList<TableRow> rows)
    {
        Title = title;
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>
    /// Gets the table title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets table column names.
    /// </summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>
    /// Gets table rows.
    /// </summary>
    public IReadOnlyList<TableRow> Rows { get; }
}

/// <summary>
/// Represents one row in a <see cref="TableModel"/>.
/// </summary>
public sealed class TableRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableRow"/> class.
    /// </summary>
    public TableRow(IReadOnlyList<string> cells)
    {
        Cells = cells ?? throw new ArgumentNullException(nameof(cells));
    }

    /// <summary>
    /// Gets row cells.
    /// </summary>
    public IReadOnlyList<string> Cells { get; }
}
