using System.Collections.Generic;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;

namespace DecisionDiagramSharp.Diagnostics.Tests;

[TestClass]
public sealed class DiagnosticExtensionTests
{
    /// <summary>
    /// Verifies that BDD diagnostic extension methods build DOT and all table models from an owned handle.
    /// </summary>
    /// <remarks>
    /// Confirms handle-first diagnostics so users do not have to manually pass the owning manager.
    /// Each extension must return a model with the expected title, confirming delegation to the correct diagnostic.
    /// </remarks>
    [TestMethod]
    public void BddExtensions_BuildDotAndTablesFromOwnedHandle()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.Var(a) & !manager.Var(b);

        // Act
        var dot = expression.ToDot();
        var nodeTable = expression.ToNodeTable();
        var truthTable = expression.ToTruthTable(new TruthTableOptions { MaxVariables = 2, MaxRows = 4 });
        var modelTable = expression.ToModelTable(new ModelEnumerationOptions { MaxModels = 4 });
        var statisticsTable = expression.ToStatisticsTable();

        // Assert
        StringAssert.Contains(dot, "digraph BDD");
        Assert.AreEqual("BDD Node Table", nodeTable.Title);
        Assert.AreEqual("BDD Truth Table", truthTable.Title);
        Assert.AreEqual("BDD Models", modelTable.Title);
        Assert.AreEqual("BDD Statistics", statisticsTable.Title);
    }

    /// <summary>
    /// Verifies that ZDD diagnostic extension methods build DOT and all table models from an owned handle.
    /// </summary>
    /// <remarks>
    /// Confirms that ZDD diagnostics support the same handle-first workflow as BDD diagnostics.
    /// </remarks>
    [TestMethod]
    public void ZddExtensions_BuildDotAndTablesFromOwnedHandle()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var family = manager.MakeFamily(new[] { new[] { a }, new[] { a, b } });

        // Act
        var dot = family.ToDot();
        var nodeTable = family.ToNodeTable();
        var setFamilyTable = family.ToSetFamilyTable(new SetEnumerationOptions { MaxSets = 4 });
        var statisticsTable = family.ToStatisticsTable();

        // Assert
        StringAssert.Contains(dot, "digraph ZDD");
        Assert.AreEqual("ZDD Node Table", nodeTable.Title);
        Assert.AreEqual("ZDD Set Family", setFamilyTable.Title);
        Assert.AreEqual("ZDD Statistics", statisticsTable.Title);
    }

    /// <summary>
    /// Verifies that diagnostic extensions reject default (uninitialized) handles with an actionable error.
    /// </summary>
    /// <remarks>
    /// Covers the invalid default-handle edge case; extensions must throw DiagramException
    /// rather than failing with a NullReferenceException.
    /// </remarks>
    [TestMethod]
    public void Extensions_RejectDefaultHandlesWithActionableError()
    {
        // Arrange
        var uninitializedBdd = default(Bdd);
        var uninitializedZdd = default(Zdd);

        // Act / Assert
        Assert.Throws<DiagramException>(() => uninitializedBdd.ToDot());
        Assert.Throws<DiagramException>(() => uninitializedZdd.ToDot());
    }
}
