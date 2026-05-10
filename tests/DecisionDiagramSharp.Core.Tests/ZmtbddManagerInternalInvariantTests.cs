using System;
using System.Collections;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

/// <summary>
/// White-box internal invariant tests for ZmtbddManager.
/// These tests depend on private implementation details and are intentionally fragile.
/// They are isolated here so that public API specification tests remain refactoring-safe.
/// </summary>
[TestClass]
[TestCategory("WhiteBox")]
[TestCategory("InternalInvariant")]
[TestCategory("Fragile")]
public sealed class ZmtbddManagerInternalInvariantTests
{
    /// <summary>
    /// Verifies that ZMTBDD enforces node limits and catches corrupted internal invariants during validation.
    /// </summary>
    /// <remarks>
    /// Confirms DiagramSizeLimitExceededException fires before memory is exhausted, and that Validate
    /// detects High==0 violations, out-of-range references, ordering violations, and unique-table corruption.
    /// Size limit test is a public API test; corruption tests are white-box.
    /// </remarks>
    [TestMethod]
    public void Zmtbdd_SizeLimitAndCorruptedStateValidation_Throw()
    {
        // Arrange / Act / Assert — size limit (public API boundary)
        var limited = new ZmtbddManager(new DecisionDiagramOptions { MaxNodeCount = 0 });
        limited.GetOrAddVariable("A");
        Assert.Throws<DiagramSizeLimitExceededException>(() => limited.Create(new[] { 1, 1 }));

        // Arrange / Act / Assert — High == 0 invariant (white-box corruption)
        var highZero = CreateThreeVariableManager(out var variables);
        _ = highZero.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(highZero, 0, CreateNode(variables[0].Value, -1, -1));
        Assert.Throws<DiagramException>(() => highZero.Validate());

        // Arrange / Act / Assert — out-of-range child (white-box corruption)
        var outOfRange = CreateThreeVariableManager(out var outOfRangeVariables);
        _ = outOfRange.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(outOfRange, 0, CreateNode(outOfRangeVariables[0].Value, -999, -1));
        Assert.Throws<DiagramException>(() => outOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation (white-box corruption)
        var ordering = CreateThreeVariableManager(out var orderingVariables);
        _ = ordering.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(ordering, 0, CreateNode(orderingVariables[1].Value, 1, -2));
        Assert.Throws<InvalidVariableOrderingException>(() => ordering.Validate());

        // Arrange / Act / Assert — unique table cleared (white-box corruption)
        var unique = CreateThreeVariableManager(out _);
        _ = unique.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        ((IDictionary)TestHelpers.GetPrivateField(unique, "_uniqueTable")).Clear();
        Assert.Throws<DiagramException>(() => unique.Validate());
    }

    /// <summary>
    /// Verifies that ZMTBDD private unique-table key implements value equality used by canonicalization.
    /// </summary>
    /// <remarks>
    /// Covers the ZmtbddKey struct's Equals and GetHashCode methods; incorrect equality would break
    /// unique-table lookups and produce duplicate canonical nodes.
    /// </remarks>
    [TestMethod]
    public void Zmtbdd_PrivateKeyTypes_ObjectEqualsAndHashCode_Work()
    {
        // Arrange
        var keyType = typeof(ZmtbddManager).GetNestedType("ZmtbddKey", BindingFlags.NonPublic)!;
        var first = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, -1, -2 }, null)!;
        var second = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, -1, -2 }, null)!;
        var third = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, -1, -2 }, null)!;

        // Act
        var equalsObject = keyType.GetMethod("Equals", new[] { typeof(object) })!;

        // Assert
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ZmtbddManager CreateThreeVariableManager(out VariableId[] variables)
    {
        var manager = new ZmtbddManager();
        variables = new[]
        {
            manager.GetOrAddVariable("A"),
            manager.GetOrAddVariable("B"),
            manager.GetOrAddVariable("C")
        };
        return manager;
    }

    private static void SetNode(ZmtbddManager manager, int index, object node)
    {
        var nodes = (IList)TestHelpers.GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(ZmtbddManager).GetNestedType("ZmtbddNode", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }
}
