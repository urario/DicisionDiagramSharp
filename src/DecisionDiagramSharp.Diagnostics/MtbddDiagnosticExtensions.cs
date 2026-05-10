using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Provides handle-first diagnostics helpers for MTBDD values.
/// </summary>
public static class MtbddDiagnosticExtensions
{
    /// <summary>
    /// Builds a DOT graph for the MTBDD value.
    /// </summary>
    public static string ToDot(this Mtbdd value)
    {
        return MtbddDiagnostics.ToDot(value.Manager, value);
    }

    /// <summary>
    /// Builds a node table for the MTBDD value.
    /// </summary>
    public static TableModel ToNodeTable(this Mtbdd value)
    {
        return MtbddDiagnostics.BuildNodeTable(value.Manager, value);
    }

    /// <summary>
    /// Builds a bounded value table for the MTBDD value.
    /// </summary>
    public static TableModel ToValueTable(this Mtbdd value, TruthTableOptions? options = null)
    {
        return MtbddDiagnostics.BuildValueTable(value.Manager, value, options);
    }

    /// <summary>
    /// Builds a statistics table for the MTBDD value.
    /// </summary>
    public static TableModel ToStatisticsTable(this Mtbdd value)
    {
        return MtbddDiagnostics.BuildStatisticsTable(value.Manager, value);
    }
}
