using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

/// <summary>
/// White-box internal invariant tests for BddManager.
/// These tests depend on private implementation details and are intentionally fragile.
/// They are isolated here so that public API specification tests remain refactoring-safe.
/// </summary>
[TestClass]
[TestCategory("WhiteBox")]
[TestCategory("InternalInvariant")]
[TestCategory("Fragile")]
public sealed class BddManagerInternalInvariantTests
{
    /// <summary>
    /// Verifies that Validate detects corrupted internal node state.
    /// </summary>
    /// <remarks>
    /// Covers internal invariant checks (equal children, out-of-range references, variable ordering, unique table)
    /// that protect canonicalization correctness. These tests intentionally corrupt internal state via reflection.
    /// </remarks>
    [TestMethod]
    public void InternalValidation_ShouldDetectCorruptedNodeState()
    {
        // Arrange / Act / Assert — equal children
        var managerEqualChildren = CreateManagerWithTwoVariableConjunction(out var a1, out _);
        SetNode(managerEqualChildren, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerEqualChildren.Validate());

        // Arrange / Act / Assert — out-of-range child
        var managerOutOfRange = CreateManagerWithTwoVariableConjunction(out var a2, out _);
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation
        var managerOrdering = CreateManagerWithTwoVariableConjunction(out var a3, out _);
        SetNode(managerOrdering, 0, CreateNode(a3.Value, 2, 1));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        // Arrange / Act / Assert — unique table cleared
        var managerUnique = CreateManagerWithTwoVariableConjunction(out _, out _);
        var uniqueTable = (IDictionary)TestHelpers.GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    /// <summary>
    /// Verifies that private key types (BddKey, IteKey, CountKey) implement value equality correctly.
    /// </summary>
    /// <remarks>
    /// Covers internal unique-table and cache key semantics; incorrect equality would break canonicalization and caching.
    /// </remarks>
    [TestMethod]
    public void Bdd_InternalKeyTypes_ShouldImplementValueEquality()
    {
        // Arrange / Act / Assert — BddKey
        var bddKeyType = typeof(BddManager).GetNestedType("BddKey", BindingFlags.NonPublic)!;
        var bddKey1 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey2 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey3 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        AssertPrivateObjectEquals(bddKeyType, bddKey1, bddKey2, bddKey3);

        // Arrange / Act / Assert — IteKey
        var iteKeyType = typeof(BddManager).GetNestedType("IteKey", BindingFlags.NonPublic)!;
        var iteKey1 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey2 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey3 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 3, 1, 0 }, null)!;
        AssertPrivateObjectEquals(iteKeyType, iteKey1, iteKey2, iteKey3);

        // Arrange / Act / Assert — CountKey
        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var countKey1 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey2 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey3 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 3 }, null)!;
        AssertPrivateObjectEquals(countKeyType, countKey1, countKey2, countKey3);
    }

    /// <summary>
    /// Verifies that private recursive helpers (CountModelsNode, EnumerateModelsRecursive, IsReachable) cover rare branches.
    /// </summary>
    /// <remarks>
    /// Covers memoization re-entry and limit enforcement inside private recursive helpers that are unreachable from the public API in isolation.
    /// </remarks>
    [TestMethod]
    public void InternalHelpers_ShouldCoverEdgeCaseBranches()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var expression = manager.Var(a);

        var countModelsNode = typeof(BddManager).GetMethod("CountModelsNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var memoType = typeof(Dictionary<,>).MakeGenericType(countKeyType, typeof(long));
        var memo = Activator.CreateInstance(memoType)!;
        var expressionNodeId = GetBddNodeId(expression);

        // Act — first call populates memo; second call hits the memoized branch
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));

        // Act / Assert — EnumerateModelsRecursive throws when seed result already exceeds limit
        var enumerateModelsRecursive = typeof(BddManager).GetMethod("EnumerateModelsRecursive", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = Assert.Throws<TargetInvocationException>(() =>
            enumerateModelsRecursive.Invoke(
                manager,
                new object[]
                {
                    1,
                    1,
                    new bool[manager.VariableCount],
                    new List<IReadOnlyDictionary<VariableId, bool>> { new Dictionary<VariableId, bool>() },
                    0
                }));
        Assert.IsInstanceOfType(target.InnerException, typeof(DiagramEnumerationLimitExceededException));

        // Act / Assert — IsReachable returns false for a node outside the diagram
        var isReachable = typeof(BddManager).GetMethod("IsReachable", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.IsFalse((bool)isReachable.Invoke(manager, new object[] { expressionNodeId, 999 })!);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static BddManager CreateManagerWithTwoVariableConjunction(out VariableId a, out VariableId b)
    {
        var manager = new BddManager();
        a = manager.GetOrAddVariable("A");
        b = manager.GetOrAddVariable("B");
        _ = manager.And(manager.Var(a), manager.Var(b));
        return manager;
    }

    private static void SetNode(BddManager manager, int index, object node)
    {
        var nodes = (IList)TestHelpers.GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(BddManager).GetNestedType("BddNode", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }

    internal static int GetBddNodeId(Bdd value)
    {
        var property = typeof(Bdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void AssertPrivateObjectEquals(Type type, object first, object second, object third)
    {
        var equalsObject = type.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }
}
