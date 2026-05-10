using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Builds diagnostics outputs for ZMTBDD values.
/// </summary>
public static class ZmtbddDiagnostics
{
    /// <summary>
    /// Builds a deterministic DOT representation for a ZMTBDD root.
    /// </summary>
    public static string ToDot(ZmtbddManager manager, Zmtbdd root)
    {
        manager.Validate(root);
        var nodes = manager.GetReachableNodeViews(root);
        var terminals = manager.GetReachableTerminalValues(root);
        var sb = new StringBuilder();
        sb.AppendLine("digraph ZMTBDD {");
        sb.AppendLine("  rankdir=TB;");
        sb.AppendLine("  node [shape=circle];");
        for (var i = 0; i < terminals.Count; i++)
        {
            sb.AppendLine("  t" + i + " [label=\"" + terminals[i].ToString(CultureInfo.InvariantCulture) + "\", shape=box];");
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            sb.AppendLine("  n" + node.NodeId + " [label=\"" + EscapeDotLabel(manager.GetVariableName(node.Variable)) + "\"];");
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            sb.AppendLine("  n" + node.NodeId + " -> " + FormatNodeId(manager, node.LowNodeId, terminals) + " [style=dashed,label=\"0\"];");
            sb.AppendLine("  n" + node.NodeId + " -> " + FormatNodeId(manager, node.HighNodeId, terminals) + " [style=solid,label=\"1\"];");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a node table diagnostics model for a ZMTBDD root.
    /// </summary>
    public static TableModel BuildNodeTable(ZmtbddManager manager, Zmtbdd root)
    {
        var nodes = manager.GetReachableNodeViews(root);
        var rows = new List<TableRow>(nodes.Count);
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            rows.Add(new TableRow(new[]
            {
                node.NodeId.ToString(CultureInfo.InvariantCulture),
                manager.GetVariableName(node.Variable),
                node.LowNodeId.ToString(CultureInfo.InvariantCulture),
                node.HighNodeId.ToString(CultureInfo.InvariantCulture)
            }));
        }

        return new TableModel("ZMTBDD Node Table", new[] { "NodeId", "Variable", "Low", "High" }, rows);
    }

    /// <summary>
    /// Builds a bounded value table for a ZMTBDD root.
    /// </summary>
    public static TableModel BuildValueTable(ZmtbddManager manager, Zmtbdd root, TruthTableOptions? options = null)
    {
        var effective = options ?? new TruthTableOptions();
        ValidateTruthTableOptions(effective);
        var rowCount = ValidateBounds(manager.VariableCount, effective);
        var columns = BuildColumns(manager);
        var rows = new List<TableRow>(rowCount);
        for (var mask = 0; mask < rowCount; mask++)
        {
            var assignment = BuildAssignment(manager.VariableCount, mask);
            var cells = BuildInputCells(manager.VariableCount, assignment);
            cells.Add(manager.Evaluate(root, assignment).ToString(CultureInfo.InvariantCulture));
            rows.Add(new TableRow(cells));
        }

        return new TableModel("ZMTBDD Value Table", columns, rows);
    }

    /// <summary>
    /// Builds a statistics table for a ZMTBDD root.
    /// </summary>
    public static TableModel BuildStatisticsTable(ZmtbddManager manager, Zmtbdd root)
    {
        var stats = manager.GetStatistics(root);
        return new TableModel("ZMTBDD Statistics", new[] { "Name", "Value" }, new[]
        {
            new TableRow(new[] { "ReachableNodeCount", stats.ReachableNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "ReachableTerminalCount", stats.ReachableTerminalCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "TotalNodeCount", stats.TotalNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "VariableCount", stats.VariableCount.ToString(CultureInfo.InvariantCulture) })
        });
    }

    private static string FormatNodeId(ZmtbddManager manager, int nodeId, IReadOnlyList<int> sortedTerminalValues)
    {
        if (nodeId >= 0)
        {
            return "n" + nodeId;
        }

        var value = manager.GetTerminalValueByNodeId(nodeId);
        for (var i = 0; i < sortedTerminalValues.Count; i++)
        {
            if (sortedTerminalValues[i] == value)
            {
                return "t" + i;
            }
        }

        return "t0";
    }

    private static int ValidateBounds(int variableCount, TruthTableOptions options)
    {
        if (variableCount > options.MaxVariables)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Value table generation exceeded MaxVariables (" + options.MaxVariables + "). Increase TruthTableOptions.MaxVariables or reduce the variable set.");
        }

        var rowCount = PowerOfTwo(variableCount);
        if (rowCount > options.MaxRows)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Value table generation exceeded MaxRows (" + options.MaxRows + "). Increase TruthTableOptions.MaxRows or reduce the variable set.");
        }

        return rowCount;
    }

    private static List<string> BuildColumns(ZmtbddManager manager)
    {
        var columns = new List<string>(manager.VariableCount + 1);
        for (var i = 0; i < manager.VariableCount; i++)
        {
            columns.Add(manager.GetVariableName(new VariableId(i)));
        }

        columns.Add("Result");
        return columns;
    }

    private static List<string> BuildInputCells(int variableCount, IReadOnlyDictionary<VariableId, bool> assignment)
    {
        var cells = new List<string>(variableCount + 1);
        for (var variable = 0; variable < variableCount; variable++)
        {
            cells.Add(assignment[new VariableId(variable)] ? "True" : "False");
        }

        return cells;
    }

    private static void ValidateTruthTableOptions(TruthTableOptions options)
    {
        if (options.MaxVariables <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(options), "TruthTableOptions.MaxVariables must be greater than zero.");
        }

        if (options.MaxRows <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(options), "TruthTableOptions.MaxRows must be greater than zero.");
        }
    }

    private static Dictionary<VariableId, bool> BuildAssignment(int variableCount, int mask)
    {
        var assignment = new Dictionary<VariableId, bool>();
        for (var i = 0; i < variableCount; i++)
        {
            assignment.Add(new VariableId(i), (mask & (1 << i)) != 0);
        }

        return assignment;
    }

    private static int PowerOfTwo(int exponent)
    {
        var value = 1;
        for (var i = 0; i < exponent; i++)
        {
            value *= 2;
        }

        return value;
    }

    private static string EscapeDotLabel(string label)
    {
        return label.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
