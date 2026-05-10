using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Provides handle-first diagnostics helpers for ZDD values.
/// </summary>
public static class ZddDiagnosticExtensions
{
    /// <summary>
    /// Builds a DOT graph for the ZDD value.
    /// </summary>
    public static string ToDot(this Zdd value)
    {
        return ZddDiagnostics.ToDot(GetManager(value), value);
    }

    /// <summary>
    /// Builds a node table for the ZDD value.
    /// </summary>
    public static TableModel ToNodeTable(this Zdd value)
    {
        return ZddDiagnostics.BuildNodeTable(GetManager(value), value);
    }

    /// <summary>
    /// Builds a bounded set-family table for the ZDD value.
    /// </summary>
    public static TableModel ToSetFamilyTable(this Zdd value, SetEnumerationOptions? options = null)
    {
        return ZddDiagnostics.BuildSetFamilyTable(GetManager(value), value, options);
    }

    /// <summary>
    /// Builds a statistics table for the ZDD value.
    /// </summary>
    public static TableModel ToStatisticsTable(this Zdd value)
    {
        return ZddDiagnostics.BuildStatisticsTable(GetManager(value), value);
    }

    private static ZddManager GetManager(Zdd value)
    {
        if (value.Manager == null)
        {
            throw new DiagramException(
                "The ZDD value is not associated with a ZddManager. Create ZDD values through ZddManager or DecisionDiagramManager.Zdd before requesting diagnostics.");
        }

        return value.Manager;
    }
}
