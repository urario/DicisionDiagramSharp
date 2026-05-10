using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class MtbddManagerTests
{
    [TestMethod]
    public void CreateAndEvaluate_MatchesNaiveIntegerTruthTable()
    {
        // Purpose: Verify that MTBDD construction preserves ordinary integer-valued truth-table semantics.
        // Arrange
        var manager = CreateThreeVariableManager(out var variables);
        var values = new[] { 7, 7, -2, -2, 3, 4, 3, 4 };

        // Act
        var function = manager.Create(values);

        // Assert
        for (var mask = 0; mask < values.Length; mask++)
        {
            Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)));
        }
    }

    [TestMethod]
    public void Canonicalization_ReducesEqualChildrenAndInternsTerminals()
    {
        // Purpose: Verify MTBDD reduction and terminal interning are observable through handles and statistics.
        // Arrange
        var manager = CreateThreeVariableManager(out var variables);
        var values = new[] { 42, 42, 42, 42, 42, 42, 42, 42 };

        // Act
        var function = manager.Create(values);
        var sameConstant = manager.Constant(42);
        var stats = manager.GetStatistics(function);

        // Assert
        Assert.AreEqual(sameConstant, function);
        Assert.AreEqual(0, manager.NonTerminalNodeCount);
        Assert.AreEqual(1, manager.TerminalCount);
        Assert.AreEqual(42, manager.Evaluate(function, BuildAssignment(variables, 7)));
        Assert.AreEqual(42, manager.GetTerminalValue(function));
        Assert.AreEqual(42, manager.GetTerminalValueByNodeId(GetNodeId(function)));
        Assert.IsTrue(function.IsTerminal);
        _ = function.GetHashCode();
        Assert.AreEqual(0, stats.ReachableNodeCount);
        Assert.AreEqual(1, stats.ReachableTerminalCount);
    }

    [TestMethod]
    public void Handles_NodeViews_Statistics_AndValidation_Work()
    {
        // Purpose: Verify MTBDD typed handles, diagnostics views, statistics, and validation form a coherent public API.
        // Arrange
        var manager = CreateThreeVariableManager(out _);
        var values = new[] { 0, 1, 2, 3, 0, 1, 2, 3 };

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
        Assert.IsFalse(function.Equals("not-mtbdd"));
        Assert.IsTrue(function == manager.Create(values));
        Assert.IsFalse(function != manager.Create(values));
        Assert.AreEqual("Mtbdd(" + GetNodeId(function) + ")", function.ToString());
        Assert.IsTrue(terminals.Contains(0));
        Assert.IsTrue(terminals.Contains(3));
        Assert.AreEqual(views.Count, stats.ReachableNodeCount);
        Assert.AreEqual(terminals.Count, stats.ReachableTerminalCount);

        var first = views[0];
        var copy = new MtbddNodeView(first.NodeId, first.Variable, first.LowNodeId, first.HighNodeId);
        Assert.AreEqual(first.NodeId, copy.NodeId);
        Assert.AreEqual(first.Variable, copy.Variable);
        Assert.AreEqual(first.LowNodeId, copy.LowNodeId);
        Assert.AreEqual(first.HighNodeId, copy.HighNodeId);
    }

    [TestMethod]
    public void InvalidInputsAndManagerMismatch_ThrowActionableExceptions()
    {
        // Purpose: Verify MTBDD rejects malformed truth tables, assignments, variables, and cross-manager handles.
        // Arrange
        var left = CreateThreeVariableManager(out var variables);
        var right = CreateThreeVariableManager(out _);
        var leftFunction = left.Create(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
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
        // Purpose: Verify MTBDD enforces node limits and catches corrupted internal invariants during validation.
        // Arrange
        var limited = new MtbddManager(new DecisionDiagramOptions { MaxNodeCount = 0 });
        limited.GetOrAddVariable("A");

        // Act and Assert
        Assert.Throws<DiagramSizeLimitExceededException>(() => limited.Create(new[] { 0, 1 }));

        var equalChildren = CreateThreeVariableManager(out var variables);
        _ = equalChildren.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(equalChildren, 0, CreateNode(variables[0].Value, -1, -1));
        Assert.Throws<DiagramException>(() => equalChildren.Validate());

        var outOfRange = CreateThreeVariableManager(out var outOfRangeVariables);
        _ = outOfRange.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(outOfRange, 0, CreateNode(outOfRangeVariables[0].Value, -999, -1));
        Assert.Throws<DiagramException>(() => outOfRange.Validate());

        var ordering = CreateThreeVariableManager(out var orderingVariables);
        _ = ordering.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(ordering, 0, CreateNode(orderingVariables[1].Value, 1, -1));
        Assert.Throws<InvalidVariableOrderingException>(() => ordering.Validate());

        var unique = CreateThreeVariableManager(out _);
        _ = unique.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        ((IDictionary)GetPrivateField(unique, "_uniqueTable")).Clear();
        Assert.Throws<DiagramException>(() => unique.Validate());
    }

    [TestMethod]
    public void RandomizedConstruction_MatchesNaiveIntegerTruthTables()
    {
        // Purpose: Verify MTBDD construction against many small generated integer truth tables.
        // Arrange
        var random = new Random(20260508);

        // Act and Assert
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var manager = CreateThreeVariableManager(out var variables);
            var values = new int[8];
            for (var mask = 0; mask < values.Length; mask++)
            {
                values[mask] = random.Next(-3, 4);
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
        // Purpose: Verify MTBDD private unique-table keys preserve value equality semantics used by canonicalization.
        // Arrange
        var keyType = typeof(MtbddManager).GetNestedType("MtbddKey", BindingFlags.NonPublic)!;
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

    private static MtbddManager CreateThreeVariableManager(out VariableId[] variables)
    {
        var manager = new MtbddManager();
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

    private static int GetNodeId(Mtbdd value)
    {
        var property = typeof(Mtbdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void SetNode(MtbddManager manager, int index, object node)
    {
        var nodes = (IList)GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(MtbddManager).GetNestedType("MtbddNode", BindingFlags.NonPublic)!;
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
