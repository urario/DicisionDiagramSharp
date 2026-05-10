namespace DecisionDiagramSharp;

/// <summary>
/// Provides a single entry point for working with the BDD, ZDD, MTBDD, and ZMTBDD managers.
/// </summary>
/// <remarks>
/// The contained managers remain semantically separate. BDD, ZDD, MTBDD, and ZMTBDD
/// values cannot be combined with each other, and each value is still owned by the manager
/// that created it.
/// </remarks>
public sealed class DecisionDiagramManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionDiagramManager"/> class.
    /// </summary>
    /// <param name="options">Optional shared options for the contained managers.</param>
    public DecisionDiagramManager(DecisionDiagramOptions? options = null)
    {
        Options = options ?? new DecisionDiagramOptions();
        Bdd = new BddManager(Options);
        Zdd = new ZddManager(Options);
        Mtbdd = new MtbddManager(Options);
        Zmtbdd = new ZmtbddManager(Options);
    }

    /// <summary>
    /// Gets the shared options used by the contained managers.
    /// </summary>
    public DecisionDiagramOptions Options { get; }

    /// <summary>
    /// Gets the BDD manager for Boolean functions.
    /// </summary>
    public BddManager Bdd { get; }

    /// <summary>
    /// Gets the ZDD manager for sparse set families.
    /// </summary>
    public ZddManager Zdd { get; }

    /// <summary>
    /// Gets the MTBDD manager for integer-valued Boolean-domain functions.
    /// </summary>
    public MtbddManager Mtbdd { get; }

    /// <summary>
    /// Gets the ZMTBDD manager for sparse integer-valued Boolean-domain functions.
    /// </summary>
    public ZmtbddManager Zmtbdd { get; }
}
