using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class ZmtbddManagerTests
{
    /// <summary>
    /// Verifies that ZMTBDD construction preserves sparse integer truth-table values under zero-suppressed semantics.
    /// </summary>
    /// <remarks>
    /// Confirms that Create maps each assignment to the expected sparse integer value for all 8 assignments;
    /// non-zero entries must survive and zero entries must be handled by zero-suppression.
    /// </remarks>
    [TestMethod]
    public void CreateAndEvaluate_MatchesSparseIntegerTruthTable()
    {
        // Arrange
        var manager = CreateThreeVariableManager(out var variables);
        var values = new[] { 5, 0, 0, -2, 0, 0, 3, 0 };

        // Act
        var function = manager.Create(values);

        // Assert
        for (var mask = 0; mask < values.Length; mask++)
        {
            Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)),
                $"mask={mask}: Evaluate must return the sparse truth-table value.");
        }
    }

    /// <summary>
    /// Verifies that ZMTBDD uses High==0 suppression and differs from MTBDD Low==High reduction.
    /// </summary>
    /// <remarks>
    /// Confirms the defining behavioral difference: ZMTBDD suppresses branches whose High child is the zero terminal,
    /// whereas MTBDD would keep a node where Low==High but both are non-zero.
    /// </remarks>
    [TestMethod]
    public void ZeroSuppression_RemovesHighZeroBranchButKeepsLowEqualsHighBranch()
    {
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
        Assert.AreEqual(0, highZero.NonTerminalNodeCount,
            "High==0 suppression must eliminate the A node for [7, 0].");
        Assert.AreEqual(7, falseA);
        Assert.AreEqual(0, trueA);
        Assert.AreEqual(1, lowEqualsHigh.NonTerminalNodeCount,
            "Low==High does not suppress when both are non-zero; B node must remain for [5, 5].");
        Assert.AreEqual(5, falseB);
        Assert.AreEqual(5, trueB);
        Assert.IsTrue(highZeroFunction.IsTerminal);
        Assert.AreEqual(7, highZero.GetTerminalValue(highZeroFunction));
        _ = highZeroFunction.GetHashCode();
    }

    /// <summary>
    /// Verifies that ZMTBDD typed handles, diagnostics views, statistics, and validation form a coherent public API.
    /// </summary>
    /// <remarks>
    /// Confirms that all structural-inspection APIs return consistent results for the same function
    /// and that the Validate method accepts a correct diagram without throwing.
    /// </remarks>
    [TestMethod]
    public void Zmtbdd_Handles_NodeViews_Statistics_AndValidation_Work()
    {
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
        Assert.IsTrue(Regex.IsMatch(function.ToString(), @"^Zmtbdd\(\d+\)$"),
            $"ToString must match 'Zmtbdd(<id>)' format; got: {function.ToString()}");
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

    /// <summary>
    /// Verifies that ZMTBDD rejects malformed truth tables, assignments, variables, and cross-manager handles.
    /// </summary>
    /// <remarks>
    /// Guards all API contract violations so callers receive actionable exceptions rather than silent failures.
    /// </remarks>
    [TestMethod]
    public void Zmtbdd_InvalidInputsAndManagerMismatch_ThrowActionableExceptions()
    {
        // Arrange
        var left = CreateThreeVariableManager(out var variables);
        var right = CreateThreeVariableManager(out _);
        var leftFunction = left.Create(new[] { 1, 0, 0, 4, 0, 0, 7, 0 });
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
        Assert.Throws<ArgumentException>(() => left.GetTerminalValueByNodeId(left.GetReachableNodeViews(leftFunction)[0].NodeId));
    }

    /// <summary>
    /// Verifies that ZMTBDD construction matches naive sparse truth tables over many randomized inputs.
    /// </summary>
    /// <remarks>
    /// Provides broad behavioral coverage for Create and Evaluate across 50 randomly generated
    /// three-variable sparse integer functions; catches systematic zero-suppression errors.
    /// </remarks>
    [TestMethod]
    public void RandomizedConstruction_MatchesNaiveSparseTruthTables()
    {
        // Arrange
        var random = new Random(20260508);

        for (var iteration = 0; iteration < 50; iteration++)
        {
            var manager = CreateThreeVariableManager(out var variables);
            var values = new int[8];
            for (var mask = 0; mask < values.Length; mask++)
            {
                values[mask] = random.NextDouble() < 0.55d ? 0 : random.Next(-3, 4);
            }

            // Act
            var function = manager.Create(values);

            // Assert
            for (var mask = 0; mask < values.Length; mask++)
            {
                Assert.AreEqual(values[mask], manager.Evaluate(function, BuildAssignment(variables, mask)),
                    $"iteration={iteration}, mask={mask}: Evaluate must return the sparse truth-table value.");
            }
        }
    }

    /// <summary>
    /// Verifies that ZMTBDD construction matches naive sparse truth tables for 1- and 2-variable functions.
    /// </summary>
    /// <remarks>
    /// Complements the 3-variable test by covering smaller variable counts; sparse distributions
    /// (55% zero probability) exercise zero-suppression at minimal and moderate scale.
    /// Seed 20260508, 30 iterations per variable count.
    /// </remarks>
    [TestMethod]
    public void RandomizedConstruction_MatchesNaiveSparseTruthTables_OneAndTwoVariables()
    {
        // Arrange
        var random = new Random(20260508);

        foreach (var varCount in new[] { 1, 2 })
        {
            var rowCount = 1 << varCount;
            for (var iteration = 0; iteration < 30; iteration++)
            {
                var manager = new ZmtbddManager();
                var variables = new VariableId[varCount];
                for (var v = 0; v < varCount; v++)
                {
                    variables[v] = manager.GetOrAddVariable(((char)('A' + v)).ToString());
                }

                var values = new int[rowCount];
                for (var mask = 0; mask < rowCount; mask++)
                {
                    values[mask] = random.NextDouble() < 0.55d ? 0 : random.Next(-3, 4);
                }

                // Act
                var function = manager.Create(values);

                // Assert
                for (var mask = 0; mask < rowCount; mask++)
                {
                    Assert.AreEqual(values[mask],
                        manager.Evaluate(function, TestHelpers.BuildBoolAssignment(variables, mask)),
                        $"seed=20260508, varCount={varCount}, iteration={iteration}, mask={mask}: Evaluate must return the sparse truth-table value.");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a ZMTBDD function with all-zero values reduces to the zero terminal only.
    /// </summary>
    /// <remarks>
    /// Confirms that an all-zero function collapses to a single terminal, validating the
    /// zero-suppressed reduction property at its extreme case.
    /// </remarks>
    [TestMethod]
    public void AllZeroFunction_ShouldBeZeroTerminalOnly()
    {
        // Arrange
        var manager = new ZmtbddManager();
        manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");

        // Act — all rows are zero
        var function = manager.Create(new[] { 0, 0, 0, 0 });
        var stats = manager.GetStatistics(function);

        // Assert
        Assert.IsTrue(function.IsZero,
            "All-zero function must reduce to the zero terminal.");
        Assert.AreEqual(0, stats.ReachableNodeCount,
            "No non-terminal nodes should exist for an all-zero function.");
    }

    /// <summary>
    /// Verifies that a ZMTBDD constant non-zero function evaluates correctly for all assignments
    /// and retains internal nodes because ZMTBDD only suppresses High==Zero.
    /// </summary>
    /// <remarks>
    /// Confirms that ZMTBDD does NOT apply the Low==High reduction used by MTBDD.
    /// ZMTBDD only suppresses nodes whose high child is the zero terminal.
    /// A function mapping all assignments to 5 has non-zero High edges and therefore keeps its internal nodes.
    /// All four evaluations must return 5.
    /// </remarks>
    [TestMethod]
    public void ConstantNonZeroFunction_ShouldEvaluateToConstantForAllAssignments()
    {
        // Arrange
        var manager = new ZmtbddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");

        // Act — all rows are 5; ZMTBDD retains nodes because High != Zero
        var function = manager.Create(new[] { 5, 5, 5, 5 });

        // Assert — every assignment evaluates to 5
        Assert.AreEqual(5, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, false }, { b, false } }));
        Assert.AreEqual(5, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, true },  { b, false } }));
        Assert.AreEqual(5, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, false }, { b, true  } }));
        Assert.AreEqual(5, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, true },  { b, true  } }));
    }

    /// <summary>
    /// Verifies that a ZMTBDD function with a single non-zero entry produces a minimal sparse structure.
    /// </summary>
    /// <remarks>
    /// Confirms that zero-suppression produces a smaller representation than a dense MTBDD would for the same function.
    /// The input [7, 0, 0, 0] has only one non-zero entry; all four evaluations must be correct.
    /// </remarks>
    [TestMethod]
    public void SingleNonZeroEntry_ShouldProduceMinimalSparseStructure()
    {
        // Arrange
        var manager = new ZmtbddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");

        // Act — only A=false, B=false → 7; all other assignments → 0
        var function = manager.Create(new[] { 7, 0, 0, 0 });
        var stats = manager.GetStatistics(function);

        // Assert — correct evaluation for all 4 assignments
        Assert.AreEqual(7, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, false }, { b, false } }));
        Assert.AreEqual(0, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, true },  { b, false } }));
        Assert.AreEqual(0, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, false }, { b, true  } }));
        Assert.AreEqual(0, manager.Evaluate(function, new Dictionary<VariableId, bool> { { a, true },  { b, true  } }));

        // Assert — node count is reduced compared to a dense representation
        Assert.IsLessThan(3, stats.ReachableNodeCount,
            "Single non-zero entry should produce a smaller structure than a fully dense two-variable ZMTBDD.");
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
        return TestHelpers.BuildBoolAssignment(variables, mask);
    }

}
