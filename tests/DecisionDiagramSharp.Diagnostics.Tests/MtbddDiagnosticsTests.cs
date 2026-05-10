using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class MtbddDiagnosticsTests
{
    /// <summary>
    /// Verifies that MTBDD diagnostics expose DOT, node, value, and statistics models with expected observable content.
    /// </summary>
    /// <remarks>
    /// Confirms that all four diagnostic outputs contain the expected identifiers, titles, and values
    /// so consumers can embed them in documentation or compare them in golden tests.
    /// </remarks>
    [TestMethod]
    public void Mtbdd_DotNodeValueAndStatisticsTables_ContainExpectedObservableContent()
    {
        // Arrange
        var manager = new MtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");
        var function = manager.Create(new[] { 10, -1, 10, 3 });

        // Act
        var dot = MtbddDiagnostics.ToDot(manager, function);
        var nodeTable = MtbddDiagnostics.BuildNodeTable(manager, function);
        var valueTable = MtbddDiagnostics.BuildValueTable(manager, function);
        var stats = MtbddDiagnostics.BuildStatisticsTable(manager, function);

        // Assert
        StringAssert.Contains(dot, "digraph MTBDD");
        StringAssert.Contains(dot, "label=\"A\"");
        StringAssert.Contains(dot, "label=\"10\"");
        Assert.AreEqual("MTBDD Node Table", nodeTable.Title);
        Assert.AreEqual("MTBDD Value Table", valueTable.Title);
        Assert.AreEqual("10", valueTable.Rows[0].Cells[2]);
        Assert.AreEqual("-1", valueTable.Rows[1].Cells[2]);
        Assert.AreEqual("MTBDD Statistics", stats.Title);
    }

    /// <summary>
    /// Verifies that the MTBDD value table respects variable and row bounds and validates options.
    /// </summary>
    /// <remarks>
    /// Confirms that the MTBDD value-table diagnostics use the same bounded-enumeration safety rules as BDD truth tables,
    /// preventing runaway table generation for large diagrams.
    /// </remarks>
    [TestMethod]
    public void Mtbdd_ValueTable_RespectsBoundsAndValidatesOptions()
    {
        // Arrange
        var manager = new MtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");
        var function = manager.Create(new[] { 1, 2, 3, 4 });

        // Act / Assert
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => MtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 1, MaxRows = 4 }));
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => MtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 2, MaxRows = 2 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => MtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 0, MaxRows = 4 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => MtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 2, MaxRows = 0 }));
    }
}
