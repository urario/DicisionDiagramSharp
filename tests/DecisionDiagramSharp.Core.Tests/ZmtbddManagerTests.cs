using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class ZmtbddManagerTests
{
    [TestMethod]
    public void CreateAndEvaluate_MatchesSparseIntegerTruthTable()
    {
        // Purpose: Verify ZMTBDD construction preserves sparse integer truth-table values under zero-suppressed semantics.
        // Arrange
        var manager = CreateThreeVariableManager(out var variables);
        var values = new[] { 5, 0, 0, -2, 0, 0, 3, 0 };

        // Act
        var function = manager.Create(values);

        // Assert
        for (var mask = 0; mask < values.Length; mask++)
        {
            Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)));
        }
    }

    [TestMethod]
    public void ZeroSuppression_RemovesHighZeroBranchButKeepsLowEqualsHighBranch()
    {
        // Purpose: Verify ZMTBDD uses High == 0 suppression and intentionally differs from MTBDD Low == High reduction.
        // Arrange
        var highZero = new ZmtbddManager();
        var a = highZero.GetOrAddVariable("A");
        var highZeroFunction = highZero.Create(new[] { 7, 0 });

        var lowEqualsHigh = new ZmtbddManager();
        var b = lowEqualsHigh.GetOrAddVariable("B");
        var lowEqualsHighFunction = lowEqualsHigh.Create(new[] { 5, 5 });

        // Act
        var falseA = highZero.Evaluate(highZeroFunction, new Dictionary<VariableId, bool> { { a, false } });
        var trueA = highZero.Evaluate(highZeroFunction, new Dictionary<VariableId, bool> { { a, true } });
        var falseB = lowEqualsHigh.Evaluate(lowEqualsHighFunction, new Dictionary<VariableId, bool> { { b, false } });
        var trueB = lowEqualsHigh.Evaluate(lowEqualsHighFunction, new Dictionary<VariableId, bool> { { b, true } });

        // Assert
        Assert.AreEqual(0, highZero.NonTerminalNodeCount);
        Assert.AreEqual(7, falseA);
        Assert.AreEqual(0, trueA);
        Assert.AreEqual(1, lowEqualsHigh.NonTerminalNodeCount);
        Assert.AreEqual(5, falseB);
        Assert.AreEqual(5, trueB);
        Assert.IsTrue(highZeroFunction.IsTerminal);
        Assert.AreEqual(7, highZero.GetTerminalValueByNodeId(GetNodeId(highZeroFunction)));
        _ = highZeroFunction.GetHashCode();
    }

    [TestMethod]
    public void Handles_NodeViews_Statistics_AndValidation_Work()
    {
        // Purpose: Verify ZMTBDD typed handles, diagnostics views, statistics, and validation form a coherent public API.
        // Arrange
        var manager = CreateThreeVariableManager(out _);
        var values = new[] { 0, 1, 0, 1, 2, 0, 2, 0 };

        // Act
        var function = manager.Create(values);
        manager.Validate();
        manager.Validate(function);
        var views = manager.GetReachableNodeViews(function);
        var terminals = manager.GetReachableTerminalValues(function);
        var stats = manager.GetStatistics(function);

        // Assert
        Assert.IsNotEmpty(views);
        Assert.AreEqual(function, manager.Create(values));
        Assert.IsTrue(function.Equals((object)function));
        Assert.IsFalse(function.Equals("not-zmtbdd"));
        Assert.IsTrue(function == manager.Create(values));
        Assert.IsFalse(function != manager.Create(values));
        Assert.AreEqual("Zmtbdd(" + GetNodeId(function) + ")", function.ToString());
        Assert.IsTrue(manager.Zero.IsZero);
        Assert.AreEqual(3, manager.TerminalCount);
        Assert.IsFalse(function.IsZero);
        Assert.IsTrue(terminals.Contains(0));
        Assert.IsTrue(terminals.Contains(2));
        Assert.AreEqual(views.Count, stats.ReachableNodeCount);
        Assert.AreEqual(terminals.Count, stats.ReachableTerminalCount);

        var first = views[0];
        var copy = new ZmtbddNodeView(first.NodeId, first.Variable, first.LowNodeId, first.HighNodeId);
        Assert.AreEqual(first.NodeId, copy.NodeId);
        Assert.AreEqual(first.Variable, copy.Variable);
        Assert.AreEqual(first.LowNodeId, copy.LowNodeId);
        Assert.AreEqual(first.HighNodeId, copy.HighNodeId);
    }

    [TestMethod]
    public void InvalidInputsAndManagerMismatch_ThrowActionableExceptions()
    {
        // Purpose: Verify ZMTBDD rejects malformed truth tables, assignments, variables, and cross-manager handles.
        // Arrange
        var left = CreateThreeVariableManager(out var variables);
        var right = CreateThreeVariableManager(out _);
        var leftFunction = left.Create(new[] { 1, 0, 0, 4, 0, 0, 7, 0 });
        var rightFunction = right.Constant(1);

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => left.GetOrAddVariable(null!));
        Assert.Throws<ArgumentNullException>(() => left.Create(null!));
        Assert.Throws<ArgumentException>(() => left.Create(new[] { 1, 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => left.Evaluate(leftFunction, null!));
        Assert.Throws<ArgumentException>(() => left.Evaluate(leftFunction, new Dictionary<VariableId, bool>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => left.GetVariableName(new VariableId(99)));
        Assert.Throws<DiagramManagerMismatchException>(() => left.Evaluate(rightFunction, BuildAssignment(variables, 0)));
        Assert.Throws<DiagramManagerMismatchException>(() => left.Validate(rightFunction));
        Assert.Throws<InvalidOperationException>(() => left.GetTerminalValue(leftFunction));
        Assert.Throws<ArgumentException>(() => left.GetTerminalValueByNodeId(GetNodeId(leftFunction)));
    }

    [TestMethod]
    public void SizeLimitAndCorruptedStateValidation_Throw()
    {
        // Purpose: Verify ZMTBDD enforces node limits and catches corrupted internal invariants during validation.
        // Arrange
        var limited = new ZmtbddManager(new DecisionDiagramOptions { MaxNodeCount = 0 });
        limited.GetOrAddVariable("A");

        // Act and Assert
        Assert.Throws<DiagramSizeLimitExceededException>(() => limited.Create(new[] { 1, 1 }));

        var highZero = CreateThreeVariableManager(out var variables);
        _ = highZero.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(highZero, 0, CreateNode(variables[0].Value, -1, -1));
        Assert.Throws<DiagramException>(() => highZero.Validate());

        var outOfRange = CreateThreeVariableManager(out var outOfRangeVariables);
        _ = outOfRange.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(outOfRange, 0, CreateNode(outOfRangeVariables[0].Value, -999, -1));
        Assert.Throws<DiagramException>(() => outOfRange.Validate());

        var ordering = CreateThreeVariableManager(out var orderingVariables);
        _ = ordering.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        SetNode(ordering, 0, CreateNode(orderingVariables[1].Value, 1, -2));
        Assert.Throws<InvalidVariableOrderingException>(() => ordering.Validate());

        var unique = CreateThreeVariableManager(out _);
        _ = unique.Create(new[] { 0, 1, 0, 1, 2, 0, 2, 0 });
        ((IDictionary)GetPrivateField(unique, "_uniqueTable")).Clear();
        Assert.Throws<DiagramException>(() => unique.Validate());
    }

    [TestMethod]
    public void RandomizedConstruction_MatchesNaiveSparseTruthTables()
    {
        // Purpose: Verify ZMTBDD construction against many small generated sparse integer truth tables.
        // Arrange
        var random = new Random(20260508);

        // Act and Assert
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var manager = CreateThreeVariableManager(out var variables);
            var values = new int[8];
            for (var mask = 0; mask < values.Length; mask++)
            {
                values[mask] = random.NextDouble() < 0.55d ? 0 : random.Next(-3, 4);
            }

            var function = manager.Create(values);
            for (var mask = 0; mask < values.Length; mask++)
            {
                Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)));
            }
        }
    }

    [TestMethod]
    public void PrivateKeyTypes_ObjectEqualsAndHashCode_Work()
    {
        // Purpose: Verify ZMTBDD private unique-table keys preserve value equality semantics used by canonicalization.
        // Arrange
        var keyType = typeof(ZmtbddManager).GetNestedType("ZmtbddKey", BindingFlags.NonPublic)!;
        var first = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { 1, -1, -2 },
            null)!;
        var second = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { 1, -1, -2 },
            null)!;
        var third = Activator.CreateInstance(
            keyType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { 2, -1, -2 },
            null)!;

        // Act
        var equalsObject = keyType.GetMethod("Equals", new[] { typeof(object) })!;

        // Assert
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode());
    }

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

    private static Dictionary<VariableId, bool> BuildAssignment(IReadOnlyList<VariableId> variables, int mask)
    {
        var assignment = new Dictionary<VariableId, bool>();
        for (var i = 0; i < variables.Count; i++)
        {
            assignment[variables[i]] = (mask & (1 << i)) != 0;
        }

        return assignment;
    }

    private static int GetNodeId(Zmtbdd value)
    {
        var property = typeof(Zmtbdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void SetNode(ZmtbddManager manager, int index, object node)
    {
        var nodes = (IList)GetPrivateField(manager, "_nodes");
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

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return field.GetValue(target)!;
    }
}
