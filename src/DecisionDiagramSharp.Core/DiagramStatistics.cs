namespace DecisionDiagramSharp;

/// <summary>
/// Provides high-level statistics for a diagram root.
/// </summary>
public sealed class DiagramStatistics
{
    /// <summary>
    /// Gets or sets the number of reachable non-terminal nodes.
    /// </summary>
    public int ReachableNodeCount { get; set; }

    /// <summary>
    /// Gets or sets the number of reachable terminal nodes.
    /// </summary>
    public int ReachableTerminalCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of non-terminal nodes managed by the manager.
    /// </summary>
    public int TotalNodeCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of distinct variables currently registered.
    /// </summary>
    public int VariableCount { get; set; }
}
