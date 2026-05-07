using DecisionDiagramSharp.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class BddDiagnosticsTests
{
    [TestMethod]
    public void DotOutput_IsDeterministic()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        var first = NormalizeNewLines(BddDiagnostics.ToDot(manager, expression));
        var second = NormalizeNewLines(BddDiagnostics.ToDot(manager, expression));

        Assert.AreEqual(first, second);
        StringAssert.Contains(first, "n0 [label=\"False\", shape=box];");
        StringAssert.Contains(first, "n1 [label=\"True\", shape=box];");
        StringAssert.Contains(first, "[label=\"A\"]");
        StringAssert.Contains(first, "[label=\"B\"]");
    }

    [TestMethod]
    public void TruthModelNodeAndStatisticsTables_AreGenerated()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        var truthTable = BddDiagnostics.BuildTruthTable(manager, expression);
        var modelTable = BddDiagnostics.BuildModelTable(manager, expression);
        var nodeTable = BddDiagnostics.BuildNodeTable(manager, expression);
        var statsTable = BddDiagnostics.BuildStatisticsTable(manager, expression);

        Assert.AreEqual("BDD Truth Table", truthTable.Title);
        Assert.AreEqual("BDD Models", modelTable.Title);
        Assert.AreEqual("BDD Node Table", nodeTable.Title);
        Assert.AreEqual("BDD Statistics", statsTable.Title);
        Assert.HasCount(4, truthTable.Rows);
        Assert.HasCount(1, modelTable.Rows);
        Assert.IsNotEmpty(nodeTable.Rows);
        Assert.HasCount(4, statsTable.Rows);
    }

    [TestMethod]
    public void TruthTableOptions_AreBounded()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Var(b));

        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxVariables = 1 }));
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxRows = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxVariables = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BddDiagnostics.BuildTruthTable(manager, expression, new TruthTableOptions { MaxRows = 0 }));
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
