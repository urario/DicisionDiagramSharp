using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class MtbddDiagnosticsTests
{
    [TestMethod]
    public void DotNodeValueAndStatisticsTables_AreDeterministic()
    {
        // Purpose: Verify MTBDD diagnostics expose DOT, node, value, and statistics models with stable observable content.
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

    [TestMethod]
    public void ValueTable_RespectsBoundsAndValidatesOptions()
    {
        // Purpose: Verify MTBDD value-table diagnostics use the same bounded-enumeration safety rules as BDD truth tables.
        // Arrange
        var manager = new MtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");
        var function = manager.Create(new[] { 1, 2, 3, 4 });

        // Act and Assert
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
