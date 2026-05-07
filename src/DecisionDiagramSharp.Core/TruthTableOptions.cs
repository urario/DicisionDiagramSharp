namespace DecisionDiagramSharp;

/// <summary>
/// Options controlling bounded BDD truth-table generation.
/// </summary>
public sealed class TruthTableOptions
{
    /// <summary>
    /// Gets or sets the maximum number of variables allowed in a generated truth table.
    /// </summary>
    public int MaxVariables { get; set; } = 16;

    /// <summary>
    /// Gets or sets the maximum number of rows allowed in a generated truth table.
    /// </summary>
    public int MaxRows { get; set; } = 65536;
}
