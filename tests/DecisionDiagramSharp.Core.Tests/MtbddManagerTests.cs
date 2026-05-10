using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class MtbddManagerTests
{
    /// <summary>
    /// Verifies that MTBDD construction preserves ordinary integer-valued truth-table semantics.
    /// </summary>
    /// <remarks>
    /// Confirms that Create maps each assignment to the expected integer value for all 8 assignments
    /// of a three-variable function.
    /// </remarks>
    [TestMethod]
    public void CreateAndEvaluate_MatchesNaiveIntegerTruthTable()
    {
        // Arrange
        var manager = CreateThreeVariableManager(out var variables);
        var values = new[] { 7, 7, -2, -2, 3, 4, 3, 4 };

        // Act
        var function = manager.Create(values);

        // Assert
        for (var mask = 0; mask < values.Length; mask++)
        {
            Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)),
                $"mask={mask}: Evaluate must return the value specified in the truth table.");
        }
    }

    /// <summary>
    /// Verifies that MTBDD reduction eliminates equal-children nodes and interns terminal values.
    /// </summary>
    /// <remarks>
    /// Confirms the Low==High reduction rule reduces a constant function to a single terminal
    /// and that Constant returns the same canonical handle.
    /// </remarks>
    [TestMethod]
    public void Canonicalization_ReducesEqualChildrenAndInternsTerminals()
    {
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

    /// <summary>
    /// Verifies that MTBDD typed handles, diagnostics views, statistics, and validation form a coherent public API.
    /// </summary>
    /// <remarks>
    /// Confirms that all structural-inspection APIs return consistent results for the same function
    /// and that the Validate method accepts a correct diagram without throwing.
    /// </remarks>
    [TestMethod]
    public void Handles_NodeViews_Statistics_AndValidation_Work()
    {
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

    /// <summary>
    /// Verifies that MTBDD rejects malformed truth tables, assignments, variables, and cross-manager handles.
    /// </summary>
    /// <remarks>
    /// Guards all API contract violations so callers receive actionable exceptions rather than silent failures.
    /// </remarks>
    [TestMethod]
    public void InvalidInputsAndManagerMismatch_ThrowActionableExceptions()
    {
        // Arrange
        var left = CreateThreeVariableManager(out var variables);
        var right = CreateThreeVariableManager(out _);
        var leftFunction = left.Create(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var rightFunction = right.Constant(1);

        // Act / Assert
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

    /// <summary>
    /// Verifies that MTBDD enforces node limits and catches corrupted internal invariants during validation.
    /// </summary>
    /// <remarks>
    /// Confirms DiagramSizeLimitExceededException fires before memory is exhausted, and that Validate
    /// detects equal-children nodes, out-of-range references, ordering violations, and unique-table corruption.
    /// </remarks>
    [TestMethod]
    public void SizeLimitAndCorruptedStateValidation_Throw()
    {
        // Arrange / Act / Assert — size limit
        var limited = new MtbddManager(new DecisionDiagramOptions { MaxNodeCount = 0 });
        limited.GetOrAddVariable("A");
        Assert.Throws<DiagramSizeLimitExceededException>(() => limited.Create(new[] { 0, 1 }));

        // Arrange / Act / Assert — equal children
        var equalChildren = CreateThreeVariableManager(out var variables);
        _ = equalChildren.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(equalChildren, 0, CreateNode(variables[0].Value, -1, -1));
        Assert.Throws<DiagramException>(() => equalChildren.Validate());

        // Arrange / Act / Assert — out-of-range child
        var outOfRange = CreateThreeVariableManager(out var outOfRangeVariables);
        _ = outOfRange.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(outOfRange, 0, CreateNode(outOfRangeVariables[0].Value, -999, -1));
        Assert.Throws<DiagramException>(() => outOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation
        var ordering = CreateThreeVariableManager(out var orderingVariables);
        _ = ordering.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        SetNode(ordering, 0, CreateNode(orderingVariables[1].Value, 1, -1));
        Assert.Throws<InvalidVariableOrderingException>(() => ordering.Validate());

        // Arrange / Act / Assert — unique table cleared
        var unique = CreateThreeVariableManager(out _);
        _ = unique.Create(new[] { 0, 1, 2, 3, 0, 1, 2, 3 });
        ((IDictionary)TestHelpers.GetPrivateField(unique, "_uniqueTable")).Clear();
        Assert.Throws<DiagramException>(() => unique.Validate());
    }

    /// <summary>
    /// Verifies that MTBDD construction matches naive integer truth tables over many randomized inputs.
    /// </summary>
    /// <remarks>
    /// Provides broad behavioral coverage for Create and Evaluate across 50 randomly generated
    /// three-variable integer functions; catches systematic errors not visible in targeted tests.
    /// </remarks>
    [TestMethod]
    public void RandomizedConstruction_MatchesNaiveIntegerTruthTables()
    {
        // Arrange
        var random = new Random(20260508);

        for (var iteration = 0; iteration < 50; iteration++)
        {
            var manager = CreateThreeVariableManager(out var variables);
            var values = new int[8];
            for (var mask = 0; mask < values.Length; mask++)
            {
                values[mask] = random.Next(-3, 4);
            }

            // Act
            var function = manager.Create(values);

            // Assert
            for (var mask = 0; mask < values.Length; mask++)
            {
                Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)),
                    $"iteration={iteration}, mask={mask}: Evaluate must return the truth-table value.");
            }
        }
    }

    /// <summary>
    /// Verifies that MTBDD private unique-table key implements value equality used by canonicalization.
    /// </summary>
    /// <remarks>
    /// Covers the MtbddKey struct's Equals and GetHashCode methods; incorrect equality would break
    /// unique-table lookups and produce duplicate canonical nodes.
    /// </remarks>
    [TestMethod]
    public void PrivateKeyTypes_ObjectEqualsAndHashCode_Work()
    {
        // Arrange
        var keyType = typeof(MtbddManager).GetNestedType("MtbddKey", BindingFlags.NonPublic)!;
        var first = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, -1, -2 }, null)!;
        var second = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, -1, -2 }, null)!;
        var third = Activator.CreateInstance(keyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, -1, -2 }, null)!;

        // Act
        var equalsObject = keyType.GetMethod("Equals", new[] { typeof(object) })!;

        // Assert
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode());
    }

    /// <summary>
    /// Verifies that an MTBDD node representing a function that ignores one variable reduces to a smaller structure.
    /// </summary>
    /// <remarks>
    /// Confirms the ROBDD Low==High reduction property for MTBDD: when all truth-table rows for A=false
    /// equal the corresponding rows for A=true, the A node must be eliminated.
    /// Truth table: [1, 1, 2, 2] — A has no effect; only B determines the value.
    /// </remarks>
    [TestMethod]
    public void SkippedVariable_ShouldReduceNodeCount()
    {
        // Arrange
        var manager = new MtbddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");

        // Act — A has no effect: A=false,B=false→1; A=false,B=true→1; A=true,B=false→2; A=true,B=true→2
        var function = manager.Create(new[] { 1, 1, 2, 2 });
        var stats = manager.GetStatistics(function);

        // Assert — only the B node should exist; A node must be eliminated
        Assert.AreEqual(1, stats.ReachableNodeCount,
            "A must be eliminated by the Low==High reduction rule; only the B node should remain.");
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
        return TestHelpers.BuildBoolAssignment(variables, mask);
    }

    private static int GetNodeId(Mtbdd value)
    {
        var property = typeof(Mtbdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void SetNode(MtbddManager manager, int index, object node)
    {
        var nodes = (IList)TestHelpers.GetPrivateField(manager, "_nodes");
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
}
