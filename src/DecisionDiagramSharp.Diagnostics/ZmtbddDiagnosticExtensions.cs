using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Provides handle-first diagnostics helpers for ZMTBDD values.
/// </summary>
public static class ZmtbddDiagnosticExtensions
{
    /// <summary>
    /// Builds a DOT graph for the ZMTBDD value.
    /// </summary>
    public static string ToDot(this Zmtbdd value)
    {
        return ZmtbddDiagnostics.ToDot(value.Manager, value);
    }

    /// <summary>
    /// Builds a node table for the ZMTBDD value.
    /// </summary>
    public static TableModel ToNodeTable(this Zmtbdd value)
    {
        return ZmtbddDiagnostics.BuildNodeTable(value.Manager, value);
    }

    /// <summary>
    /// Builds a bounded value table for the ZMTBDD value.
    /// </summary>
    public static TableModel ToValueTable(this Zmtbdd value, TruthTableOptions? options = null)
    {
        return ZmtbddDiagnostics.BuildValueTable(value.Manager, value, options);
    }

    /// <summary>
    /// Builds a statistics table for the ZMTBDD value.
    /// </summary>
    public static TableModel ToStatisticsTable(this Zmtbdd value)
    {
        return ZmtbddDiagnostics.BuildStatisticsTable(value.Manager, value);
    }
}
