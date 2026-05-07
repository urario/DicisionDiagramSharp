using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DecisionDiagramSharp;

namespace DecisionDiagramSharp.Diagnostics;

/// <summary>
/// Builds diagnostics outputs for BDD values.
/// </summary>
public static class BddDiagnostics
{
    /// <summary>
    /// Builds a deterministic DOT representation for a BDD root.
    /// </summary>
    public static string ToDot(BddManager manager, Bdd root)
    {
        manager.Validate(root);
        var nodes = manager.GetReachableNodeViews(root);
        var sb = new StringBuilder();
        sb.AppendLine("digraph BDD {");
        sb.AppendLine("  rankdir=TB;");
        sb.AppendLine("  node [shape=circle];");
        sb.AppendLine("  n0 [label=\"False\", shape=box];");
        sb.AppendLine("  n1 [label=\"True\", shape=box];");

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
    /// Builds a node table diagnostics model for a BDD root.
    /// </summary>
    public static TableModel BuildNodeTable(BddManager manager, Bdd root)
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

        return new TableModel("BDD Node Table", new[] { "NodeId", "Variable", "Low", "High" }, rows);
    }

    /// <summary>
    /// Builds a bounded truth table for a BDD root.
    /// </summary>
    public static TableModel BuildTruthTable(BddManager manager, Bdd root, TruthTableOptions? options = null)
    {
        var effective = options ?? new TruthTableOptions();
        ValidateTruthTableOptions(effective);

        var variableCount = manager.VariableCount;
        if (variableCount > effective.MaxVariables)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Truth table generation exceeded MaxVariables (" + effective.MaxVariables + "). Increase TruthTableOptions.MaxVariables or reduce the variable set.");
        }

        var rowCount = PowerOfTwo(variableCount);
        if (rowCount > effective.MaxRows)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Truth table generation exceeded MaxRows (" + effective.MaxRows + "). Increase TruthTableOptions.MaxRows or reduce the variable set.");
        }

        var columns = new List<string>(variableCount + 1);
        for (var i = 0; i < variableCount; i++)
        {
            columns.Add(manager.GetVariableName(new VariableId(i)));
        }

        columns.Add("Result");

        var rows = new List<TableRow>(rowCount);
        for (var mask = 0; mask < rowCount; mask++)
        {
            var assignment = BuildAssignment(variableCount, mask);
            var cells = new List<string>(variableCount + 1);
            for (var variable = 0; variable < variableCount; variable++)
            {
                cells.Add(FormatBoolean(assignment[new VariableId(variable)]));
            }

            cells.Add(FormatBoolean(manager.Evaluate(root, assignment)));
            rows.Add(new TableRow(cells));
        }

        return new TableModel("BDD Truth Table", columns, rows);
    }

    /// <summary>
    /// Builds a bounded model table for satisfying assignments of a BDD root.
    /// </summary>
    public static TableModel BuildModelTable(BddManager manager, Bdd root, ModelEnumerationOptions? options = null)
    {
        var models = manager.EnumerateModels(root, options);
        var variableCount = manager.VariableCount;
        var columns = new List<string>(variableCount + 1) { "Index" };
        for (var i = 0; i < variableCount; i++)
        {
            columns.Add(manager.GetVariableName(new VariableId(i)));
        }

        var rows = new List<TableRow>(models.Count);
        for (var i = 0; i < models.Count; i++)
        {
            var cells = new List<string>(variableCount + 1)
            {
                i.ToString(CultureInfo.InvariantCulture)
            };
            for (var variable = 0; variable < variableCount; variable++)
            {
                cells.Add(FormatBoolean(models[i][new VariableId(variable)]));
            }

            rows.Add(new TableRow(cells));
        }

        return new TableModel("BDD Models", columns, rows);
    }

    /// <summary>
    /// Builds a statistics table for a BDD root.
    /// </summary>
    public static TableModel BuildStatisticsTable(BddManager manager, Bdd root)
    {
        var stats = manager.GetStatistics(root);
        var rows = new List<TableRow>
        {
            new TableRow(new[] { "ReachableNodeCount", stats.ReachableNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "ReachableTerminalCount", stats.ReachableTerminalCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "TotalNodeCount", stats.TotalNodeCount.ToString(CultureInfo.InvariantCulture) }),
            new TableRow(new[] { "VariableCount", stats.VariableCount.ToString(CultureInfo.InvariantCulture) })
        };

        return new TableModel("BDD Statistics", new[] { "Name", "Value" }, rows);
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

    private static string FormatBoolean(bool value)
    {
        return value ? "True" : "False";
    }

    private static string EscapeDotLabel(string label)
    {
        return label.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
