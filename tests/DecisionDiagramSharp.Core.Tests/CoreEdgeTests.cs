using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class CoreEdgeTests
{
    /// <summary>
    /// Verifies that VariableId implements value equality, comparison, and string representation correctly.
    /// </summary>
    /// <remarks>
    ///Guards the value-type contract of VariableId so it can be safely used as a dictionary key and in ordered collections.
    /// </remarks>
    [TestMethod]
    public void VariableId_EqualityAndComparisonSemantics_ShouldFollowValueType()
    {
        // Arrange
        var v1 = new VariableId(1);
        var v2 = new VariableId(1);
        var v3 = new VariableId(3);

        // Act / Assert
        Assert.IsTrue(v1 == v2);
        Assert.IsTrue(v1 != v3);
        Assert.IsTrue(v1.Equals((object)v2));
        Assert.IsFalse(v1.Equals("not-variable-id"));
        Assert.AreEqual(0, v1.CompareTo(v2));
        Assert.IsLessThan(0, v1.CompareTo(v3));
        Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
        Assert.AreEqual("1", v1.ToString());
    }

    /// <summary>
    /// Verifies that Zdd implements equality and string representation following handle semantics.
    /// </summary>
    /// <remarks>
    ///Guards that Zdd handles from the same canonical node compare equal, and that the string representation is stable.
    /// </remarks>
    [TestMethod]
    public void Zdd_EqualityAndStringRepresentation_ShouldFollowHandleSemantics()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var set = manager.MakeSet(new[] { a });
        var set2 = manager.MakeSet(new[] { a });

        // Act / Assert — same canonical node
        Assert.IsTrue(set == set2);
        Assert.IsFalse(set != set2);
        Assert.IsTrue(set.Equals((object)set2));
        Assert.IsFalse(set.Equals("not-zdd"));
        Assert.AreEqual(set.GetHashCode(), set2.GetHashCode());
        Assert.AreEqual("Zdd(2)", set.ToString());
        Assert.IsFalse(set.IsEmpty);
        Assert.IsFalse(set.IsBase);

        // Act / Assert — default (uninitialized) handle maps to Empty
        var defaultZdd = default(Zdd);
        Assert.IsTrue(defaultZdd.IsEmpty);
        Assert.IsFalse(defaultZdd.IsBase);
        Assert.AreEqual("Zdd(0)", defaultZdd.ToString());
        Assert.IsTrue(defaultZdd.Equals((object)defaultZdd));
        Assert.IsFalse(defaultZdd.Equals("default-zdd"));
        _ = defaultZdd.GetHashCode();
    }

    /// <summary>
    /// Verifies that VariableTable throws on null or empty variable names, and on unknown VariableId lookups.
    /// </summary>
    /// <remarks>
    ///Guards the API contract that variable names must be non-null, non-empty strings,
    /// and that GetName requires a registered VariableId.
    /// </remarks>
    [TestMethod]
    public void VariableTable_ArgumentValidation_ShouldThrowOnNullOrEmpty()
    {
        // Arrange
        var table = new VariableTable();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => table.GetOrAdd(null!));
        Assert.Throws<ArgumentException>(() => table.GetOrAdd(string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetName(new VariableId(10)));
    }

    /// <summary>
    /// Verifies that ZddManager throws on null arguments, unknown VariableId, and zero MaxSets.
    /// </summary>
    /// <remarks>
    ///Guards the public API contract for all argument-validation cases in ZddManager.
    /// </remarks>
    [TestMethod]
    public void ZddManager_ArgumentValidation_ShouldThrowOnNullOrInvalidInputs()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var setA = manager.MakeSet(new[] { a });

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => manager.MakeSet((IEnumerable<VariableId>)null!));
        Assert.Throws<ArgumentNullException>(() => manager.MakeFamily((IEnumerable<IEnumerable<VariableId>>)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.MakeSet(new[] { new VariableId(999) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.EnumerateSets(setA, new SetEnumerationOptions { MaxSets = 0 }));
    }

    /// <summary>
    /// Verifies that ZddManager terminal operations return consistent results.
    /// </summary>
    /// <remarks>
    ///Confirms that terminal identity operations (Union/Intersect/Difference with Empty/Base) satisfy
    /// the identity and annihilator laws for ZDD set-family algebra.
    /// </remarks>
    [TestMethod]
    public void ZddManager_TerminalOperations_ShouldReturnExpectedResults()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var setA = manager.MakeSet(new[] { a });
        Assert.IsGreaterThan(0, manager.NonTerminalNodeCount);

        // Act / Assert — identity laws with Empty
        Assert.AreEqual(setA, manager.Union(setA, manager.Empty));
        Assert.IsTrue(manager.Intersect(setA, manager.Empty).IsEmpty);
        Assert.AreEqual(setA, manager.Difference(setA, manager.Empty));
        Assert.IsTrue(manager.Difference(setA, setA).IsEmpty);

        // Act / Assert — terminal cases for subset and change operations
        Assert.IsTrue(manager.Subset1(manager.Base, a).IsEmpty);
        Assert.AreEqual(manager.Base, manager.Subset0(manager.Base, a));
        Assert.IsTrue(manager.Containing(manager.Base, a).IsEmpty);
        Assert.AreEqual(manager.Base, manager.NotContaining(manager.Base, a));
        Assert.IsTrue(manager.Change(manager.Empty, a).IsEmpty);

        // Act / Assert — membership
        Assert.IsTrue(manager.ContainsSet(setA, new[] { a }));
        Assert.IsFalse(manager.ContainsSet(setA, new[] { b }));
    }

    /// <summary>
    /// Verifies that ZddNodeView exposes its fields as immutable, constructor-initialized values.
    /// </summary>
    /// <remarks>
    ///Guards that the public ZddNodeView struct correctly stores and exposes diagnostic fields.
    /// </remarks>
    [TestMethod]
    public void ZddNodeView_ShouldExposeImmutableFields()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { a, c } });

        // Act
        var views = manager.GetReachableNodeViews(family);
        var first = views[0];
        var copy = new ZddNodeView(first.NodeId, first.Variable, first.LowNodeId, first.HighNodeId);

        // Assert
        Assert.AreEqual(first.NodeId, copy.NodeId);
        Assert.AreEqual(first.Variable, copy.Variable);
        Assert.AreEqual(first.LowNodeId, copy.LowNodeId);
        Assert.AreEqual(first.HighNodeId, copy.HighNodeId);
    }

    /// <summary>
    /// Verifies that DiagramStatistics exposes the configured values passed to its constructor.
    /// </summary>
    /// <remarks>
    ///Guards that the DiagramStatistics struct correctly stores all four fields.
    /// </remarks>
    [TestMethod]
    public void DiagramStatistics_ShouldExposeConfiguredValues()
    {
        // Arrange / Act
        var stats = new DiagramStatistics
        {
            ReachableNodeCount = 1,
            ReachableTerminalCount = 2,
            TotalNodeCount = 3,
            VariableCount = 4
        };

        // Assert
        Assert.AreEqual(1, stats.ReachableNodeCount);
        Assert.AreEqual(2, stats.ReachableTerminalCount);
        Assert.AreEqual(3, stats.TotalNodeCount);
        Assert.AreEqual(4, stats.VariableCount);
    }

    /// <summary>
    /// Verifies that Change correctly toggles a variable that does not appear at or above the root.
    /// </summary>
    /// <remarks>
    ///Covers the branch where the pivot variable's position is above the current root during Change traversal.
    /// </remarks>
    [TestMethod]
    public void ZddManager_Change_ShouldToggleVariableAboveRoot()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var setA = manager.MakeSet(new[] { a });

        // Act — toggle B which does not appear in setA
        var toggled = manager.Change(setA, b);
        var sets = manager.EnumerateSets(toggled);

        // Assert — result is {A, B}
        Assert.HasCount(1, sets);
        var values = new HashSet<int> { sets[0][0].Value, sets[0][1].Value };
        Assert.Contains(a.Value, values);
        Assert.Contains(b.Value, values);
    }

    /// <summary>
    /// Verifies that GetStatistics on terminal roots reports zero reachable non-terminal nodes.
    /// </summary>
    /// <remarks>
    ///Covers the terminal-root branches in the reachability walk, which differ from non-terminal roots.
    /// </remarks>
    [TestMethod]
    public void ZddManager_Statistics_OnTerminalRoots_ShouldReportZeroReachableNodes()
    {
        // Arrange
        var manager = new ZddManager();

        // Act
        var emptyStats = manager.GetStatistics(manager.Empty);
        var baseStats = manager.GetStatistics(manager.Base);

        // Assert
        Assert.AreEqual(0, emptyStats.ReachableNodeCount);
        Assert.AreEqual(1, emptyStats.ReachableTerminalCount);
        Assert.AreEqual(0, baseStats.ReachableNodeCount);
        Assert.AreEqual(1, baseStats.ReachableTerminalCount);
    }

}
