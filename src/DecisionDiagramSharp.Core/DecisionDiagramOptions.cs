namespace DecisionDiagramSharp;

/// <summary>
/// Defines global limits and behavior options for decision diagram managers.
/// </summary>
public sealed class DecisionDiagramOptions
{
    /// <summary>
    /// Gets or sets the maximum number of non-terminal nodes allowed in a manager.
    /// </summary>
    /// <remarks>
    /// The limit protects callers from unbounded diagram growth.
    /// </remarks>
    public int MaxNodeCount { get; set; } = 1_000_000;
}
