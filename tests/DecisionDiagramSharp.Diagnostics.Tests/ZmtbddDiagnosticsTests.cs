using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class ZmtbddDiagnosticsTests
{
    /// <summary>
    /// Verifies that ZMTBDD diagnostics expose DOT, node, value, and statistics models with stable observable content.
    /// </summary>
    /// <remarks>
    /// Confirms that all four diagnostic outputs are deterministic and contain the expected identifiers and values
    /// so consumers can embed them in documentation or compare them in golden tests.
    /// </remarks>
    [TestMethod]
    public void DotNodeValueAndStatisticsTables_AreDeterministic()
    {
        // Arrange
        var manager = new ZmtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");
        var function = manager.Create(new[] { 10, 0, 0, 3 });

        // Act
        var dot = ZmtbddDiagnostics.ToDot(manager, function);
        var nodeTable = ZmtbddDiagnostics.BuildNodeTable(manager, function);
        var valueTable = ZmtbddDiagnostics.BuildValueTable(manager, function);
        var stats = ZmtbddDiagnostics.BuildStatisticsTable(manager, function);

        // Assert
        StringAssert.Contains(dot, "digraph ZMTBDD");
        StringAssert.Contains(dot, "label=\"A\"");
        StringAssert.Contains(dot, "label=\"0\"");
        Assert.AreEqual("ZMTBDD Node Table", nodeTable.Title);
        Assert.AreEqual("ZMTBDD Value Table", valueTable.Title);
        Assert.AreEqual("10", valueTable.Rows[0].Cells[2]);
        Assert.AreEqual("0", valueTable.Rows[1].Cells[2]);
        Assert.AreEqual("ZMTBDD Statistics", stats.Title);
    }

    /// <summary>
    /// Verifies that the ZMTBDD value table respects variable and row bounds and validates options.
    /// </summary>
    /// <remarks>
    /// Confirms that ZMTBDD value-table diagnostics use bounded-enumeration safety rules,
    /// preventing runaway table generation for large diagrams.
    /// </remarks>
    [TestMethod]
    public void ValueTable_RespectsBoundsAndValidatesOptions()
    {
        // Arrange
        var manager = new ZmtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");
        var function = manager.Create(new[] { 1, 0, 0, 4 });

        // Act / Assert
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => ZmtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 1, MaxRows = 4 }));
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => ZmtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 2, MaxRows = 2 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => ZmtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 0, MaxRows = 4 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => ZmtbddDiagnostics.BuildValueTable(manager, function, new TruthTableOptions { MaxVariables = 2, MaxRows = 0 }));
    }
}
