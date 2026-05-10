using System.Collections.Generic;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddOperatorTests
{
    /// <summary>
    /// Verifies that friendly BDD operators preserve Boolean semantics using an explicit truth table.
    /// </summary>
    /// <remarks>
    /// Confirms that the operator overloads (&amp;, |, ^, !) delegate correctly to manager methods
    /// and that evaluation matches pre-computed expected values rather than recomputed expressions.
    /// </remarks>
    [TestMethod]
    public void Operators_MatchBooleanTruthTableSemantics()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = (manager.Var(a) & !manager.Var(b)) | (manager.Var(a) ^ manager.Var(b));

        var cases = new[]
        {
            new { A = false, B = false, Expected = false },
            new { A = true,  B = false, Expected = true  },
            new { A = false, B = true,  Expected = true  },
            new { A = true,  B = true,  Expected = false },
        };

        foreach (var c in cases)
        {
            // Act
            var assignment = new Dictionary<VariableId, bool> { { a, c.A }, { b, c.B } };
            var actual = manager.Evaluate(expression, assignment);

            // Assert
            Assert.AreEqual(c.Expected, actual,
                $"A={c.A}, B={c.B}: result should match the explicit truth table.");
        }
    }

    /// <summary>
    /// Verifies that operator overloads reject operands from different managers.
    /// </summary>
    /// <remarks>
    /// Confirms that operator overloads keep the manager-ownership safety boundary intact,
    /// matching the behavior of the underlying manager methods.
    /// </remarks>
    [TestMethod]
    public void Operators_RejectCrossManagerOperands()
    {
        // Arrange
        var leftManager = new BddManager();
        var rightManager = new BddManager();
        var left = leftManager.Var(leftManager.GetOrAddVariable("A"));
        var right = rightManager.Var(rightManager.GetOrAddVariable("A"));

        // Act / Assert
        Assert.Throws<DiagramManagerMismatchException>(() => left & right);
        Assert.Throws<DiagramManagerMismatchException>(() => left | right);
        Assert.Throws<DiagramManagerMismatchException>(() => left ^ right);
    }

    /// <summary>
    /// Verifies that an uninitialized (default) BDD handle reports a manager-ownership error on operator use.
    /// </summary>
    /// <remarks>
    /// Confirms that the default handle fails with a clear ownership error rather than a NullReferenceException,
    /// providing an actionable message to callers who forget to initialize their BDD variable.
    /// </remarks>
    [TestMethod]
    public void Operators_RejectDefaultHandle()
    {
        // Arrange
        var value = default(Bdd);

        // Act / Assert
        Assert.Throws<DiagramManagerMismatchException>(() => !value);
    }
}
