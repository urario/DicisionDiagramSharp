using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Provides handle-first diagnostics helpers for BDD values.
/// </summary>
public static class BddDiagnosticExtensions
{
    /// <summary>
    /// Builds a DOT graph for the BDD value.
    /// </summary>
    public static string ToDot(this Bdd value)
    {
        return BddDiagnostics.ToDot(GetManager(value), value);
    }

    /// <summary>
    /// Builds a node table for the BDD value.
    /// </summary>
    public static TableModel ToNodeTable(this Bdd value)
    {
        return BddDiagnostics.BuildNodeTable(GetManager(value), value);
    }

    /// <summary>
    /// Builds a bounded truth table for the BDD value.
    /// </summary>
    public static TableModel ToTruthTable(this Bdd value, TruthTableOptions? options = null)
    {
        return BddDiagnostics.BuildTruthTable(GetManager(value), value, options);
    }

    /// <summary>
    /// Builds a bounded satisfying-model table for the BDD value.
    /// </summary>
    public static TableModel ToModelTable(this Bdd value, ModelEnumerationOptions? options = null)
    {
        return BddDiagnostics.BuildModelTable(GetManager(value), value, options);
    }

    /// <summary>
    /// Builds a statistics table for the BDD value.
    /// </summary>
    public static TableModel ToStatisticsTable(this Bdd value)
    {
        return BddDiagnostics.BuildStatisticsTable(GetManager(value), value);
    }

    private static BddManager GetManager(Bdd value)
    {
        if (value.Manager == null)
        {
            throw new DiagramException(
                "The BDD value is not associated with a BddManager. Create BDD values through BddManager or DecisionDiagramManager.Bdd before requesting diagnostics.");
        }

        return value.Manager;
    }
}
