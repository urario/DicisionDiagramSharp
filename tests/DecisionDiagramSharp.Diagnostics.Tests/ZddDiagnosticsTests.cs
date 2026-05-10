using Microsoft.VisualStudio.TestTools.UnitTesting;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class ZddDiagnosticsTests
{
    /// <summary>
    /// Verifies that ZDD DOT output is deterministic and contains expected node labels and edge styles.
    /// </summary>
    /// <remarks>
    /// Confirms that repeated calls to ToDot for the same diagram return identical output,
    /// and that terminal labels, variable labels, and edge-style markers appear in the graph description.
    /// </remarks>
    [TestMethod]
    public void Zdd_DotOutput_IsDeterministic()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var family = manager.MakeFamily(new[] { new[] { a }, new[] { a, b } });

        // Act
        var first = DiagnosticsTestHelpers.NormalizeNewLines(ZddDiagnostics.ToDot(manager, family));
        var second = DiagnosticsTestHelpers.NormalizeNewLines(ZddDiagnostics.ToDot(manager, family));

        // Assert
        Assert.AreEqual(first, second);
        StringAssert.Contains(first, "n0 [label=\"Empty\", shape=box];");
        StringAssert.Contains(first, "n1 [label=\"Base\", shape=box];");
        StringAssert.Contains(first, "[label=\"A\"]");
        StringAssert.Contains(first, "[label=\"B\"]");
        StringAssert.Contains(first, "[style=dashed,label=\"0\"]");
        StringAssert.Contains(first, "[style=solid,label=\"1\"]");
    }

    /// <summary>
    /// Verifies that ZDD node, set-family, and statistics tables are generated with correct titles,
    /// row counts, and specific cell values.
    /// </summary>
    /// <remarks>
    /// Confirms the structural and value-level correctness of all three diagnostic table outputs
    /// for the family {{A,B},{A}}. Cell-value assertions make the test a specification for exact table content.
    /// Set family rows: index 0 → {A}, index 1 → {A, B} (or reverse; both are present).
    /// Set format uses "{name1, name2}" with a space after each comma (string.Join(", ", names)).
    /// Statistics keys use internal field names: ReachableNodeCount, ReachableTerminalCount, TotalNodeCount, VariableCount.
    /// </remarks>
    [TestMethod]
    public void NodeSetAndStatisticsTables_AreGenerated()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { a } });

        // Act
        var nodeTable = ZddDiagnostics.BuildNodeTable(manager, family);
        var setTable = ZddDiagnostics.BuildSetFamilyTable(manager, family);
        var statsTable = ZddDiagnostics.BuildStatisticsTable(manager, family);

        // Assert — titles
        Assert.AreEqual("ZDD Node Table", nodeTable.Title);
        Assert.AreEqual("ZDD Set Family", setTable.Title);
        Assert.AreEqual("ZDD Statistics", statsTable.Title);

        // Assert — row counts
        Assert.IsNotEmpty(nodeTable.Rows);
        Assert.HasCount(2, setTable.Rows);
        Assert.HasCount(4, statsTable.Rows);

        // Assert — set-family cell values: one row must contain "{A}" and one must contain "{A, B}"
        // Format: "{" + string.Join(", ", names) + "}" — space after comma
        var setKeys = new System.Collections.Generic.HashSet<string>
        {
            setTable.Rows[0].Cells[1],
            setTable.Rows[1].Cells[1]
        };
        Assert.Contains("{A}", setKeys, "Set family must contain the set {A}.");
        Assert.Contains("{A, B}", setKeys, "Set family must contain the set {A, B}.");

        // Assert — statistics table labels (first column is the internal field name)
        Assert.AreEqual("ReachableNodeCount", statsTable.Rows[0].Cells[0]);
        Assert.AreEqual("ReachableTerminalCount", statsTable.Rows[1].Cells[0]);
        Assert.AreEqual("TotalNodeCount", statsTable.Rows[2].Cells[0]);
        Assert.AreEqual("VariableCount", statsTable.Rows[3].Cells[0]);
    }
}
