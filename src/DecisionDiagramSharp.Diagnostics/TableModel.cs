using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Generic immutable table model used by text exporters.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TableModel"/> class.
/// </remarks>
public sealed class TableModel(string title, IReadOnlyList<string> columns, IReadOnlyList<TableRow> rows)
{

    /// <summary>
    /// Gets the table title.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets table column names.
    /// </summary>
    public IReadOnlyList<string> Columns { get; } = columns ?? throw new ArgumentNullException(nameof(columns));

    /// <summary>
    /// Gets table rows.
    /// </summary>
    public IReadOnlyList<TableRow> Rows { get; } = rows ?? throw new ArgumentNullException(nameof(rows));
}

/// <summary>
/// Represents one row in a <see cref="TableModel"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TableRow"/> class.
/// </remarks>
public sealed class TableRow(IReadOnlyList<string> cells)
{

    /// <summary>
    /// Gets row cells.
    /// </summary>
    public IReadOnlyList<string> Cells { get; } = cells ?? throw new ArgumentNullException(nameof(cells));
}
