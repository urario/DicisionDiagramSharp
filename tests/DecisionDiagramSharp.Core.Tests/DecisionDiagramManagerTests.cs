using System.Collections.Generic;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class DecisionDiagramManagerTests
{
    [TestMethod]
    public void UnifiedManager_ExposesSeparateManagersWithSharedOptions()
    {
        // Purpose: verifies that the unified manager improves discovery while preserving separate diagram semantics.
        // Arrange
        var options = new DecisionDiagramOptions { MaxNodeCount = 16 };

        // Act
        var manager = new DecisionDiagramManager(options);
        var bddVariable = manager.Bdd.GetOrAddVariable("A");
        var zddVariable = manager.Zdd.GetOrAddVariable("A");
        var mtbddVariable = manager.Mtbdd.GetOrAddVariable("A");
        var zmtbddVariable = manager.Zmtbdd.GetOrAddVariable("A");
        var bddValue = manager.Bdd.Var(bddVariable);
        var zddValue = manager.Zdd.MakeSet(new[] { zddVariable });
        var mtbddValue = manager.Mtbdd.Create(new[] { 0, 7 });
        var zmtbddValue = manager.Zmtbdd.Create(new[] { 5, 0 });

        // Assert
        Assert.AreSame(options, manager.Options);
        Assert.AreSame(options, manager.Bdd.Options);
        Assert.AreSame(options, manager.Zdd.Options);
        Assert.AreSame(options, manager.Mtbdd.Options);
        Assert.AreSame(options, manager.Zmtbdd.Options);
        Assert.IsTrue(manager.Bdd.Evaluate(bddValue, new Dictionary<VariableId, bool> { { bddVariable, true } }));
        Assert.IsTrue(manager.Zdd.ContainsSet(zddValue, new[] { zddVariable }));
        Assert.AreEqual(7, manager.Mtbdd.Evaluate(mtbddValue, new Dictionary<VariableId, bool> { { mtbddVariable, true } }));
        Assert.AreEqual(0, manager.Zmtbdd.Evaluate(zmtbddValue, new Dictionary<VariableId, bool> { { zmtbddVariable, true } }));
    }

    [TestMethod]
    public void UnifiedManager_DefaultConstructorCreatesUsableManagers()
    {
        // Purpose: proves that beginners can create one facade and immediately build values across supported diagram families.
        // Arrange
        var manager = new DecisionDiagramManager();

        // Act
        var bddVariable = manager.Bdd.GetOrAddVariable("Enabled");
        var zddVariable = manager.Zdd.GetOrAddVariable("Feature");
        var mtbddVariable = manager.Mtbdd.GetOrAddVariable("Score");
        var zmtbddVariable = manager.Zmtbdd.GetOrAddVariable("SparseScore");

        // Assert
        Assert.IsTrue(manager.Bdd.Var(bddVariable).Equals(manager.Bdd.Var("Enabled")));
        Assert.AreEqual(1L, manager.Zdd.CountSets(manager.Zdd.MakeSet(new[] { zddVariable })));
        Assert.AreEqual(3, manager.Mtbdd.Evaluate(manager.Mtbdd.Create(new[] { 2, 3 }), new Dictionary<VariableId, bool> { { mtbddVariable, true } }));
        Assert.AreEqual(0, manager.Zmtbdd.Evaluate(manager.Zmtbdd.Create(new[] { 2, 0 }), new Dictionary<VariableId, bool> { { zmtbddVariable, true } }));
    }
}
