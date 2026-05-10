using DecisionDiagramSharp.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class BddDiagnosticsTests
{
    /// <summary>
    /// Verifies that BDD DOT output is deterministic and contains expected node labels.
    /// </summary>
    /// <remarks>
    /// Confirms that repeated calls to ToDot for the same diagram return identical output,
    /// and that terminal and variable labels appear in the graph description.
    /// </remarks>
    [TestMethod]
    public void DotOutput_IsDeterministic()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        // Act
        var first = DiagnosticsTestHelpers.NormalizeNewLines(BddDiagnostics.ToDot(manager, expression));
        var second = DiagnosticsTestHelpers.NormalizeNewLines(BddDiagnostics.ToDot(manager, expression));

        // Assert
        Assert.AreEqual(first, second);
        StringAssert.Contains(first, "n0 [label=\"False\", shape=box];");
        StringAssert.Contains(first, "n1 [label=\"True\", shape=box];");
        StringAssert.Contains(first, "[label=\"A\"]");
        StringAssert.Contains(first, "[label=\"B\"]");
    }

    /// <summary>
    /// Verifies that BDD truth, model, node, and statistics tables are generated with correct titles,
    /// row counts, and specific cell values.
    /// </summary>
    /// <remarks>
    /// Confirms the structural and value-level correctness of all four diagnostic table outputs for A &amp;&amp; !B.
    /// Cell-value assertions make the test a specification for exact table content, not just shape.
    /// Row ordering: variable index 0 (A) is the least-significant bit, so A changes on every row.
    /// Truth table rows for A &amp;&amp; !B: (F,F,F), (T,F,T), (F,T,F), (T,T,F).
    /// Model table: one model {A=True, B=False}.
    /// Statistics keys use internal field names: ReachableNodeCount, ReachableTerminalCount, TotalNodeCount, VariableCount.
    /// </remarks>
    [TestMethod]
    public void TruthModelNodeAndStatisticsTables_AreGenerated()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        // Act
        var truthTable = BddDiagnostics.BuildTruthTable(manager, expression);
        var modelTable = BddDiagnostics.BuildModelTable(manager, expression);
        var nodeTable = BddDiagnostics.BuildNodeTable(manager, expression);
        var statsTable = BddDiagnostics.BuildStatisticsTable(manager, expression);

        // Assert — titles
        Assert.AreEqual("BDD Truth Table", truthTable.Title);
        Assert.AreEqual("BDD Models", modelTable.Title);
        Assert.AreEqual("BDD Node Table", nodeTable.Title);
        Assert.AreEqual("BDD Statistics", statsTable.Title);

        // Assert — row counts
        Assert.HasCount(4, truthTable.Rows);
        Assert.HasCount(1, modelTable.Rows);
        Assert.IsNotEmpty(nodeTable.Rows);
        Assert.HasCount(4, statsTable.Rows);

        // Assert — specific truth-table cell values (columns: A, B, Result)
        // Variable index 0 (A) is LSB: mask=0→(F,F), mask=1→(T,F), mask=2→(F,T), mask=3→(T,T)
        // Row 0: A=False, B=False → Result=False
        Assert.AreEqual("False", truthTable.Rows[0].Cells[0], "Row 0 A should be False.");
        Assert.AreEqual("False", truthTable.Rows[0].Cells[1], "Row 0 B should be False.");
        Assert.AreEqual("False", truthTable.Rows[0].Cells[2], "Row 0 Result for A&&!B should be False.");
        // Row 1: A=True, B=False → Result=True
        Assert.AreEqual("True", truthTable.Rows[1].Cells[0], "Row 1 A should be True.");
        Assert.AreEqual("False", truthTable.Rows[1].Cells[1], "Row 1 B should be False.");
        Assert.AreEqual("True", truthTable.Rows[1].Cells[2], "Row 1 Result for A&&!B should be True.");
        // Row 2: A=False, B=True → Result=False
        Assert.AreEqual("False", truthTable.Rows[2].Cells[0], "Row 2 A should be False.");
        Assert.AreEqual("True", truthTable.Rows[2].Cells[1], "Row 2 B should be True.");
        Assert.AreEqual("False", truthTable.Rows[2].Cells[2], "Row 2 Result for A&&!B should be False.");
        // Row 3: A=True, B=True → Result=False
        Assert.AreEqual("False", truthTable.Rows[3].Cells[2], "Row 3 Result for A&&!B should be False.");

        // Assert — specific model-table cell values (columns: Index, A, B)
        Assert.AreEqual("0", modelTable.Rows[0].Cells[0], "Model index should be 0.");
        Assert.AreEqual("True", modelTable.Rows[0].Cells[1], "Model A should be True.");
        Assert.AreEqual("False", modelTable.Rows[0].Cells[2], "Model B should be False.");

        // Assert — statistics table labels (first column is the internal field name)
        Assert.AreEqual("ReachableNodeCount", statsTable.Rows[0].Cells[0]);
        Assert.AreEqual("ReachableTerminalCount", statsTable.Rows[1].Cells[0]);
        Assert.AreEqual("TotalNodeCount", statsTable.Rows[2].Cells[0]);
        Assert.AreEqual("VariableCount", statsTable.Rows[3].Cells[0]);
    }

    /// <summary>
    /// Verifies that BDD truth table generation throws when the variable or row limit is exceeded.
    /// </summary>
    /// <remarks>
    /// Confirms that truth table generation respects MaxVariables and MaxRows limits,
    /// and that zero or negative limits are rejected with ArgumentOutOfRangeException.
    /// </remarks>
    [TestMethod]
    public void TruthTableOptions_AreBounded()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Var(b));

        // Act / Assert
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxVariables = 1 }));
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxRows = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxVariables = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxRows = 0 }));
    }
}
