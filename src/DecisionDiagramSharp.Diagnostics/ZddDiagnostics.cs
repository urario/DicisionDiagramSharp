using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Builds diagnostics outputs for ZDD values.
/// </summary>
public static class ZddDiagnostics
{
    /// <summary>
    /// Builds a deterministic DOT representation for a ZDD root.
    /// </summary>
    public static string ToDot(ZddManager manager, Zdd root)
    {
        manager.Validate(root);
        var nodes = manager.GetReachableNodeViews(root);
        var sb = new StringBuilder();
        sb.AppendLine("digraph ZDD {");
        sb.AppendLine("  rankdir=TB;");
        sb.AppendLine("  node [shape=circle];");
        sb.AppendLine("  n0 [label=\"Empty\", shape=box];");
        sb.AppendLine("  n1 [label=\"Base\", shape=box];");

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var variableName = EscapeDotLabel(manager.GetVariableName(node.Variable));
            sb.AppendLine("  n" + node.NodeId + " [label=\"" + variableName + "\"];");
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            sb.AppendLine("  n" + node.NodeId + " -> n" + node.LowNodeId + " [style=dashed,label=\"0\"];");
            sb.AppendLine("  n" + node.NodeId + " -> n" + node.HighNodeId + " [style=solid,label=\"1\"];");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a node table diagnostics model for a ZDD root.
    /// </summary>
    public static TableModel BuildNodeTable(ZddManager manager, Zdd root)
    {
        var nodes = manager.GetReachableNodeViews(root);
        var rows = new List<TableRow>(nodes.Count);
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            rows.Add(
                new TableRow(
                    new[]
                    {
                        node.NodeId.ToString(CultureInfo.InvariantCulture),
                        manager.GetVariableName(node.Variable),
                        node.LowNodeId.ToString(CultureInfo.InvariantCulture),
                        node.HighNodeId.ToString(CultureInfo.InvariantCulture)
                    }));
        }

        return new TableModel("ZDD Node Table", new[] { "NodeId", "Variable", "Low", "High" }, rows);
    }

    /// <summary>
    /// Builds a bounded set-family table for a ZDD root.
    /// </summary>
    public static TableModel BuildSetFamilyTable(ZddManager manager, Zdd root, SetEnumerationOptions? options = null)
    {
        var sets = manager.EnumerateSets(root, options);
        var rows = new List<TableRow>(sets.Count);
        for (var i = 0; i < sets.Count; i++)
        {
            var set = sets[i];
            var names = new List<string>(set.Count);
            for (var j = 0; j < set.Count; j++)
            {
                names.Add(manager.GetVariableName(set[j]));
            }

            rows.Add(
                new TableRow(
                    new[]
                    {
                        i.ToString(CultureInfo.InvariantCulture),
                        "{" + string.Join(", ", names) + "}"
                    }));
        }

        return new TableModel("ZDD Set Family", new[] { "Index", "Set" }, rows);
    }

    /// <summary>
    /// Builds a statistics table for a ZDD root.
    /// </summary>
    public static TableModel BuildStatisticsTable(ZddManager manager, Zdd root)
    {
        var stats = manager.GetStatistics(root);
        var rows = new List<TableRow>
        {
            new TableRow(new[] { "ReachableNodeCount", stats.ReachableNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "ReachableTerminalCount", stats.ReachableTerminalCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "TotalNodeCount", stats.TotalNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "VariableCount", stats.VariableCount.ToString(CultureInfo.InvariantCulture) })
        };

        return new TableModel("ZDD Statistics", new[] { "Name", "Value" }, rows);
    }

    private static string EscapeDotLabel(string label)
    {
        return label.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
