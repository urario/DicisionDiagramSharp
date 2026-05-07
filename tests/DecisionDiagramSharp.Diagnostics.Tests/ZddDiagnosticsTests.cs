using Microsoft.VisualStudio.TestTools.UnitTesting;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class ZddDiagnosticsTests
{
    [TestMethod]
    public void DotOutput_IsDeterministic()
    {
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { a },
                new[] { a, b }
            });

        var first = NormalizeNewLines(ZddDiagnostics.ToDot(manager, family));
        var second = NormalizeNewLines(ZddDiagnostics.ToDot(manager, family));
        Assert.AreEqual(first, second);
        StringAssert.Contains(first, "n0 [label=\"Empty\", shape=box];");
        StringAssert.Contains(first, "n1 [label=\"Base\", shape=box];");
        StringAssert.Contains(first, "[label=\"A\"]");
        StringAssert.Contains(first, "[label=\"B\"]");
        StringAssert.Contains(first, "[style=dashed,label=\"0\"]");
        StringAssert.Contains(first, "[style=solid,label=\"1\"]");
    }

    [TestMethod]
    public void NodeSetAndStatisticsTables_AreGenerated()
    {
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { a, b },
                new[] { a }
            });

        var nodeTable = ZddDiagnostics.BuildNodeTable(manager, family);
        var setTable = ZddDiagnostics.BuildSetFamilyTable(manager, family);
        var statsTable = ZddDiagnostics.BuildStatisticsTable(manager, family);

        Assert.AreEqual("ZDD Node Table", nodeTable.Title);
        Assert.AreEqual("ZDD Set Family", setTable.Title);
        Assert.AreEqual("ZDD Statistics", statsTable.Title);
        Assert.IsNotEmpty(nodeTable.Rows);
        Assert.HasCount(2, setTable.Rows);
        Assert.HasCount(4, statsTable.Rows);
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
