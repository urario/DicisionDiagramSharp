using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class CoreEdgeTests
{
    [TestMethod]
    public void VariableId_And_Zdd_Equality_Basics()
    {
        var v1 = new VariableId(1);
        var v2 = new VariableId(1);
        var v3 = new VariableId(3);
        Assert.IsTrue(v1 == v2);
        Assert.IsTrue(v1 != v3);
        Assert.IsTrue(v1.Equals((object)v2));
        Assert.IsFalse(v1.Equals("not-variable-id"));
        Assert.AreEqual(0, v1.CompareTo(v2));
        Assert.IsLessThan(0, v1.CompareTo(v3));
        Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
        Assert.AreEqual("1", v1.ToString());

        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var set = manager.MakeSet(new[] { a });
        var set2 = manager.MakeSet(new[] { a });
        Assert.IsTrue(set == set2);
        Assert.IsFalse(set != set2);
        Assert.IsTrue(set.Equals((object)set2));
        Assert.IsFalse(set.Equals("not-zdd"));
        Assert.AreEqual(set.GetHashCode(), set2.GetHashCode());
        Assert.AreEqual("Zdd(2)", set.ToString());
        Assert.IsFalse(set.IsEmpty);
        Assert.IsFalse(set.IsBase);

        var defaultZdd = default(Zdd);
        Assert.IsTrue(defaultZdd.IsEmpty);
        Assert.IsFalse(defaultZdd.IsBase);
        Assert.AreEqual("Zdd(0)", defaultZdd.ToString());
        Assert.IsTrue(defaultZdd.Equals((object)defaultZdd));
        Assert.IsFalse(defaultZdd.Equals("default-zdd"));
        _ = defaultZdd.GetHashCode();
    }

    [TestMethod]
    public void VariableTable_ArgumentValidation()
    {
        var table = new VariableTable();
        Assert.Throws<ArgumentNullException>(() => table.GetOrAdd(null!));
        Assert.Throws<ArgumentException>(() => table.GetOrAdd(string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetName(new VariableId(10)));
    }

    [TestMethod]
    public void Manager_ArgumentValidation_And_TerminalBranches()
    {
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var setA = manager.MakeSet(new[] { a });
        var setB = manager.MakeSet(new[] { b });
        Assert.IsGreaterThan(0, manager.NonTerminalNodeCount);

        Assert.Throws<ArgumentNullException>(() => manager.MakeSet(null!));
        Assert.Throws<ArgumentNullException>(() => manager.MakeFamily(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.MakeSet(new[] { new VariableId(999) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.EnumerateSets(setA, new SetEnumerationOptions { MaxSets = 0 }));

        Assert.AreEqual(setA, manager.Union(setA, manager.Empty));
        Assert.IsTrue(manager.Intersect(setA, manager.Empty).IsEmpty);
        Assert.AreEqual(setA, manager.Difference(setA, manager.Empty));
        Assert.IsTrue(manager.Difference(setA, setA).IsEmpty);

        Assert.IsTrue(manager.Subset1(manager.Base, a).IsEmpty);
        Assert.AreEqual(manager.Base, manager.Subset0(manager.Base, a));
        Assert.IsTrue(manager.Containing(manager.Base, a).IsEmpty);
        Assert.AreEqual(manager.Base, manager.NotContaining(manager.Base, a));
        Assert.IsTrue(manager.Change(manager.Empty, a).IsEmpty);
        Assert.IsTrue(manager.ContainsSet(setA, new[] { a }));
        Assert.IsFalse(manager.ContainsSet(setA, new[] { b }));
    }

    [TestMethod]
    public void DiagnosticsViews_And_Validation_Work()
    {
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { a, b },
                new[] { a, c }
            });

        manager.Validate();
        manager.Validate(family);

        var views = manager.GetReachableNodeViews(family);
        Assert.IsNotEmpty(views);
        var first = views[0];
        var copy = new ZddNodeView(first.NodeId, first.Variable, first.LowNodeId, first.HighNodeId);
        Assert.AreEqual(first.NodeId, copy.NodeId);
        Assert.AreEqual(first.Variable, copy.Variable);

        var stats = manager.GetStatistics(family);
        Assert.IsGreaterThan(0, stats.ReachableNodeCount);
        Assert.IsGreaterThanOrEqualTo(stats.ReachableNodeCount, stats.TotalNodeCount);

        var invalidOrdering = new InvalidVariableOrderingException("ordering");
        Assert.AreEqual("ordering", invalidOrdering.Message);
        var nakedStats = new DiagramStatistics { ReachableNodeCount = 1, ReachableTerminalCount = 2, TotalNodeCount = 3, VariableCount = 4 };
        Assert.AreEqual(4, nakedStats.VariableCount);
    }

    [TestMethod]
    public void Change_Branch_WhenVariableIsNotPresentAboveRoot()
    {
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var setA = manager.MakeSet(new[] { a });
        var toggledB = manager.Change(setA, b);
        var sets = manager.EnumerateSets(toggledB);
        Assert.HasCount(1, sets);
        var values = new HashSet<int> { sets[0][0].Value, sets[0][1].Value };
        Assert.Contains(a.Value, values);
        Assert.Contains(b.Value, values);
    }

    [TestMethod]
    public void Statistics_OnTerminalRoots_CoverReachabilityBranches()
    {
        var manager = new ZddManager();

        var emptyStats = manager.GetStatistics(manager.Empty);
        Assert.AreEqual(0, emptyStats.ReachableNodeCount);
        Assert.AreEqual(1, emptyStats.ReachableTerminalCount);

        var baseStats = manager.GetStatistics(manager.Base);
        Assert.AreEqual(0, baseStats.ReachableNodeCount);
        Assert.AreEqual(1, baseStats.ReachableTerminalCount);
    }

    [TestMethod]
    public void Validate_CatchesCorruptedInternalState()
    {
        var managerHighEmpty = new ZddManager();
        var a1 = managerHighEmpty.GetOrAddVariable("A");
        var b1 = managerHighEmpty.GetOrAddVariable("B");
        _ = managerHighEmpty.MakeSet(new[] { a1, b1 });
        SetNode(managerHighEmpty, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerHighEmpty.Validate());

        var managerOutOfRange = new ZddManager();
        var a2 = managerOutOfRange.GetOrAddVariable("A");
        var b2 = managerOutOfRange.GetOrAddVariable("B");
        _ = managerOutOfRange.MakeSet(new[] { a2, b2 });
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        var managerOrdering = new ZddManager();
        var a3 = managerOrdering.GetOrAddVariable("A");
        var b3 = managerOrdering.GetOrAddVariable("B");
        _ = managerOrdering.MakeSet(new[] { a3, b3 });
        SetNode(managerOrdering, 1, CreateNode(b3.Value, 0, 2));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        var managerUnique = new ZddManager();
        var a4 = managerUnique.GetOrAddVariable("A");
        var b4 = managerUnique.GetOrAddVariable("B");
        _ = managerUnique.MakeSet(new[] { a4, b4 });
        var uniqueTable = (IDictionary)GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    [TestMethod]
    public void PrivateKeyTypes_ObjectEqualsAndHashCode_Work()
    {
        var zddKeyType = typeof(ZddManager).GetNestedType("ZddKey", BindingFlags.NonPublic)!;
        var zddKey1 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey2 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey3 = Activator.CreateInstance(zddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        var zddKeyEqualsObject = zddKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)zddKeyEqualsObject.Invoke(zddKey1, new[] { zddKey2 })!);
        Assert.IsFalse((bool)zddKeyEqualsObject.Invoke(zddKey1, new object[] { zddKey3 })!);
        Assert.AreNotEqual(zddKey1.GetHashCode(), zddKey3.GetHashCode());

        var binaryOpKeyType = typeof(ZddManager).GetNestedType("BinaryOpKey", BindingFlags.NonPublic)!;
        var createMethod = binaryOpKeyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;
        var binaryKey1 = createMethod.Invoke(null, new object[] { 2, 1 })!;
        var binaryKey2 = createMethod.Invoke(null, new object[] { 1, 2 })!;
        var binaryKey3 = createMethod.Invoke(null, new object[] { 2, 3 })!;
        var binaryKeyEqualsObject = binaryOpKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)binaryKeyEqualsObject.Invoke(binaryKey1, new[] { binaryKey2 })!);
        Assert.IsFalse((bool)binaryKeyEqualsObject.Invoke(binaryKey1, new object[] { binaryKey3 })!);
        Assert.AreNotEqual(binaryKey1.GetHashCode(), binaryKey3.GetHashCode());
    }

    [TestMethod]
    public void EnumerateSetsRecursive_ThrowsWhenSeedResultAlreadyExceedsLimit()
    {
        var manager = new ZddManager();
        var enumerateSetsRecursive = typeof(ZddManager).GetMethod(
            "EnumerateSetsRecursive",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

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

    private static void SetNode(ZddManager manager, int index, object node)
    {
        var nodes = (IList)GetPrivateField(manager, "_nodes");
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

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return field.GetValue(target)!;
    }
}
