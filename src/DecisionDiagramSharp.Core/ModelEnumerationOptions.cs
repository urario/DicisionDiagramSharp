namespace DecisionDiagramSharp;

/// <summary>
/// Options controlling bounded BDD model enumeration.
/// </summary>
public sealed class ModelEnumerationOptions
{
    /// <summary>
    /// Gets or sets the maximum number of satisfying models to enumerate.
    /// </summary>
    public int MaxModels { get; set; } = 1000;
}
