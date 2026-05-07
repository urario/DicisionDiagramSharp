namespace DecisionDiagramSharp;

/// <summary>
/// Controls ZDD set-family enumeration limits.
/// </summary>
public sealed class SetEnumerationOptions
{
    /// <summary>
    /// Gets or sets the maximum number of sets to return.
    /// </summary>
    public int MaxSets { get; set; } = 1000;
}
