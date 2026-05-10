using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddStringApiTests
{
    /// <summary>
    /// Verifies that Var(string) returns the same canonical node as Var(VariableId).
    /// </summary>
    /// <remarks>
    /// Confirms that the string-name shorthand delegates to GetOrAddVariable and returns
    /// the identical reduced-ordered BDD node. If Var(string) creates a different node ID
    /// the unique-table invariant is violated.
    /// </remarks>
    [TestMethod]
    public void Var_StringName_ShouldReturnSameNodeAsVarById()
    {
        // Arrange
        var manager = new BddManager();
        var id = manager.GetOrAddVariable("A");

        // Act
        var byId = manager.Var(id);
        var byName = manager.Var("A");

        // Assert — canonical representation: same Boolean function → same node
        Assert.AreEqual(byId, byName, "Var(string) and Var(VariableId) must return the same canonical node.");
    }

    /// <summary>
    /// Verifies that Var(string) for a new name implicitly registers the variable.
    /// </summary>
    /// <remarks>
    /// Ensures that calling Var with an unregistered name does not throw and that
    /// the resulting node evaluates correctly, confirming registration happened.
    /// </remarks>
    [TestMethod]
    public void Var_NewStringName_ShouldRegisterVariableAndReturnNode()
    {
        // Arrange
        var manager = new BddManager();

        // Act
        var node = manager.Var("X");
        var xId = manager.GetOrAddVariable("X");
        var trueResult = manager.Evaluate(node, new Dictionary<VariableId, bool> { { xId, true } });
        var falseResult = manager.Evaluate(node, new Dictionary<VariableId, bool> { { xId, false } });

        // Assert
        Assert.IsTrue(trueResult, "X=true: Var('X') should evaluate to true.");
        Assert.IsFalse(falseResult, "X=false: Var('X') should evaluate to false.");
    }

    /// <summary>
    /// Verifies that Evaluate with a string-keyed dictionary returns the same result as the VariableId overload.
    /// </summary>
    /// <remarks>
    /// Confirms that the string-keyed convenience overload resolves names to VariableId correctly
    /// and produces specification-correct Boolean evaluation results.
    /// </remarks>
    [TestMethod]
    public void Evaluate_StringKeyedAssignment_ShouldMatchVariableIdResult()
    {
        // Arrange
        var manager = new BddManager();
        var aId = manager.GetOrAddVariable("A");
        var bId = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(aId), manager.Not(manager.Var(bId)));

        var cases = new[]
        {
            new { A = false, B = false, Expected = false },
            new { A = true,  B = false, Expected = true  },
            new { A = false, B = true,  Expected = false },
            new { A = true,  B = true,  Expected = false },
        };

        foreach (var c in cases)
        {
            // Act
            var stringAssignment = new Dictionary<string, bool> { { "A", c.A }, { "B", c.B } };
            var actual = manager.Evaluate(expr, (IReadOnlyDictionary<string, bool>)stringAssignment);

            // Assert
            Assert.AreEqual(
                c.Expected,
                actual,
                $"A={c.A}, B={c.B}: string-keyed Evaluate result should match the explicit truth table.");
        }
    }

    /// <summary>
    /// Verifies that Evaluate with a string-keyed assignment throws ArgumentException for an unknown variable name.
    /// </summary>
    /// <remarks>
    /// Guards the API contract that unrecognized variable names produce a clear error rather than
    /// silent wrong results or a KeyNotFoundException from internal lookup.
    /// </remarks>
    [TestMethod]
    public void Evaluate_UnknownVariableName_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new BddManager();
        var expr = manager.Var("A");

        // Act / Assert
        Assert.Throws<ArgumentException>(
            () => manager.Evaluate(expr, (IReadOnlyDictionary<string, bool>)new Dictionary<string, bool> { { "Z", true } }));
    }

    /// <summary>
    /// Verifies that Evaluate with a null string-keyed assignment throws ArgumentNullException.
    /// </summary>
    /// <remarks>
    /// Guards the null-safety contract for the string-keyed overload, consistent with the
    /// VariableId overload.
    /// </remarks>
    [TestMethod]
    public void Evaluate_NullStringKeyedAssignment_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = new BddManager();
        var expr = manager.Var("A");

        // Act / Assert
        Assert.Throws<ArgumentNullException>(
            () => manager.Evaluate(expr, (IReadOnlyDictionary<string, bool>)null!));
    }
}
