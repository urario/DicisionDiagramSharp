using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

/// <summary>
/// White-box internal invariant tests for ZddManager.
/// These tests depend on private implementation details and are intentionally fragile.
/// They consolidate duplicated reflection tests previously split between ZddManagerTests and CoreEdgeTests.
/// </summary>
[TestClass]
[TestCategory("WhiteBox")]
[TestCategory("InternalInvariant")]
[TestCategory("Fragile")]
public sealed class ZddManagerInternalInvariantTests
{
    /// <summary>
    /// Verifies that Validate detects corrupted internal ZDD node state.
    /// </summary>
    /// <remarks>
    /// Covers internal invariant checks (High==Empty violation, out-of-range references, variable ordering, unique table)
    /// that protect canonicalization correctness. Intentionally corrupts internal state via reflection.
    /// Consolidates duplicates previously in ZddManagerTests and CoreEdgeTests.
    /// </remarks>
    [TestMethod]
    public void InternalValidation_ShouldDetectCorruptedZddNodeState()
    {
        // Arrange / Act / Assert — High == Empty violated (both children are terminal 0)
        var managerHighEmpty = CreateManager("A", "B");
        var a1 = managerHighEmpty.GetOrAddVariable("A");
        var b1 = managerHighEmpty.GetOrAddVariable("B");
        _ = managerHighEmpty.MakeSet(new[] { a1, b1 });
        SetNode(managerHighEmpty, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerHighEmpty.Validate());

        // Arrange / Act / Assert — out-of-range child
        var managerOutOfRange = CreateManager("A", "B");
        var a2 = managerOutOfRange.GetOrAddVariable("A");
        var b2 = managerOutOfRange.GetOrAddVariable("B");
        _ = managerOutOfRange.MakeSet(new[] { a2, b2 });
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation
        var managerOrdering = CreateManager("A", "B");
        var a3 = managerOrdering.GetOrAddVariable("A");
        var b3 = managerOrdering.GetOrAddVariable("B");
        _ = managerOrdering.MakeSet(new[] { a3, b3 });
        SetNode(managerOrdering, 1, CreateNode(b3.Value, 0, 2));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        // Arrange / Act / Assert — unique table cleared
        var managerUnique = CreateManager("A", "B");
        var a4 = managerUnique.GetOrAddVariable("A");
        var b4 = managerUnique.GetOrAddVariable("B");
        _ = managerUnique.MakeSet(new[] { a4, b4 });
        var uniqueTable = (IDictionary)TestHelpers.GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    /// <summary>
    /// Verifies that private key types (ZddKey, BinaryOpKey) implement value equality correctly.
    /// </summary>
    /// <remarks>
    /// Covers internal unique-table and cache key semantics; incorrect equality would break canonicalization and caching.
    /// Consolidates duplicates previously in ZddManagerTests and CoreEdgeTests.
    /// </remarks>
    [TestMethod]
    public void Zdd_InternalKeyTypes_ShouldImplementValueEquality()
    {
        // Arrange / Act / Assert — ZddKey
        var zddKeyType = typeof(ZddManager).GetNestedType("ZddKey", BindingFlags.NonPublic)!;
        var zddKey1 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey2 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey3 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        var zddKeyEquals = zddKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)zddKeyEquals.Invoke(zddKey1, new[] { zddKey2 })!);
        Assert.IsFalse((bool)zddKeyEquals.Invoke(zddKey1, new object[] { zddKey3 })!);
        Assert.IsFalse((bool)zddKeyEquals.Invoke(zddKey1, new object[] { "not-a-key" })!);
        Assert.AreEqual(zddKey1.GetHashCode(), zddKey2.GetHashCode());

        // Arrange / Act / Assert — BinaryOpKey
        var binaryOpKeyType = typeof(ZddManager).GetNestedType("BinaryOpKey", BindingFlags.NonPublic)!;
        var createMethod = binaryOpKeyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;
        var binaryKey1 = createMethod.Invoke(null, new object[] { 2, 1 })!;
        var binaryKey2 = createMethod.Invoke(null, new object[] { 1, 2 })!;
        var binaryKey3 = createMethod.Invoke(null, new object[] { 2, 3 })!;
        var binaryKeyEquals = binaryOpKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)binaryKeyEquals.Invoke(binaryKey1, new[] { binaryKey2 })!);
        Assert.IsFalse((bool)binaryKeyEquals.Invoke(binaryKey1, new object[] { binaryKey3 })!);
        Assert.AreEqual(binaryKey1.GetHashCode(), binaryKey2.GetHashCode());
    }

    /// <summary>
    /// Verifies that EnumerateSetsRecursive throws when the seed result already exceeds the limit.
    /// </summary>
    /// <remarks>
    /// Covers the edge case where the limit is exceeded before any new sets are added during recursion.
    /// Consolidates duplicates previously in ZddManagerTests and CoreEdgeTests.
    /// </remarks>
    [TestMethod]
    public void InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit()
    {
        // Arrange
        var manager = new ZddManager();
        var enumerateSetsRecursive = typeof(ZddManager).GetMethod(
            "EnumerateSetsRecursive",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act / Assert
        var target = Assert.Throws<TargetInvocationException>(() =>
            enumerateSetsRecursive.Invoke(
                manager,
                new object[]
                {
                    1,
                    new List<VariableId>(),
                    new List<IReadOnlyList<VariableId>> { Array.Empty<VariableId>() },
                    0
                }));
        Assert.IsInstanceOfType(target.InnerException, typeof(DiagramEnumerationLimitExceededException));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ZddManager CreateManager(params string[] names)
    {
        var manager = new ZddManager();
        foreach (var name in names)
        {
            manager.GetOrAddVariable(name);
        }

        return manager;
    }

    private static void SetNode(ZddManager manager, int index, object node)
    {
        var nodes = (IList)TestHelpers.GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(ZddManager).GetNestedType("ZddNode", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }
}
