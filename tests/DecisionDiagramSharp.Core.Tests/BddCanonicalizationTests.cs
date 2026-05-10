using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddCanonicalizationTests
{
    /// <summary>
    /// Verifies that Var("A") always returns the same canonical node.
    /// </summary>
    /// <remarks>
    ///Confirms the unique table ensures structural sharing for variable nodes; node identity, not just logical equivalence.
    /// </remarks>
    [TestMethod]
    public void Var_SameVariable_ShouldReturnCanonicalNode()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");

        // Act
        var node1 = manager.Var(a);
        var node2 = manager.Var(a);

        // Assert — canonical node identity
        Assert.AreEqual(node1, node2, "Same variable must return the same canonical BDD node.");
    }

    /// <summary>
    /// Verifies that And(A, A) returns the same canonical node as Var(A).
    /// </summary>
    /// <remarks>
    ///Confirms ROBDD idempotence reduces And(A, A) to the canonical A node, not a new redundant node.
    /// </remarks>
    [TestMethod]
    public void And_SameOperand_ShouldReturnCanonicalOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.And(aNode, aNode);

        // Assert — canonical node identity
        Assert.AreEqual(aNode, result, "And(A, A) must reduce to the canonical A node.");
    }

    /// <summary>
    /// Verifies that Ite(A, True, False) == Var(A) as canonical nodes.
    /// </summary>
    /// <remarks>
    ///Confirms ITE with True then-branch and False else-branch reduces to the condition node itself, following ROBDD reduction rules.
    /// </remarks>
    [TestMethod]
    public void Ite_WithTrueAndFalseBranches_ShouldReturnCondition()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Ite(aNode, manager.True, manager.False);

        // Assert — canonical node identity
        Assert.AreEqual(aNode, result, "Ite(A, True, False) must be canonical A node.");
    }

    /// <summary>
    /// Verifies that Ite(A, X, X) == X when then and else branches are identical.
    /// </summary>
    /// <remarks>
    ///Confirms ITE with identical branches reduces to the branch node itself regardless of the condition,
    /// following the ROBDD Low==High reduction rule.
    /// </remarks>
    [TestMethod]
    public void Ite_WithIdenticalBranches_ShouldReturnBranch()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var result = manager.Ite(aNode, bNode, bNode);

        // Assert — canonical node identity: condition irrelevant when branches are equal
        Assert.AreEqual(bNode, result, "Ite(A, B, B) must equal the branch B node.");
    }

    /// <summary>
    /// Verifies Boolean operations match an explicit truth table for A && !B || A ^ B.
    /// </summary>
    /// <remarks>
    ///Confirms compound expression evaluation matches pre-computed expected values rather than recomputed logic,
    /// so the test can detect bugs where the BDD implementation agrees with the test expression but both are wrong.
    /// </remarks>
    [TestMethod]
    public void BooleanOperations_ShouldMatchExplicitTruthTable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.Or(
            manager.And(manager.Var(a), manager.Not(manager.Var(b))),
            manager.Xor(manager.Var(a), manager.Var(b)));

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
    /// Verifies that logical evaluation is independent of variable registration order.
    /// </summary>
    /// <remarks>
    ///Confirms the BDD evaluates the same Boolean function regardless of which variable was registered first.
    /// </remarks>
    [TestMethod]
    public void Evaluate_ShouldNotDependOnVariableRegistrationOrder()
    {
        // Arrange — register variables in different order
        var managerAB = new BddManager();
        var aFirst = managerAB.GetOrAddVariable("A");
        var bFirst = managerAB.GetOrAddVariable("B");

        var managerBA = new BddManager();
        var bSecond = managerBA.GetOrAddVariable("B");
        var aSecond = managerBA.GetOrAddVariable("A");

        var exprABResult = managerAB.And(managerAB.Var(aFirst), managerAB.Not(managerAB.Var(bFirst)));
        var exprBAResult = managerBA.And(managerBA.Var(aSecond), managerBA.Not(managerBA.Var(bSecond)));

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
            var assignAB = new Dictionary<VariableId, bool> { { aFirst, c.A }, { bFirst, c.B } };
            var assignBA = new Dictionary<VariableId, bool> { { aSecond, c.A }, { bSecond, c.B } };

            // Assert
            Assert.AreEqual(c.Expected, managerAB.Evaluate(exprABResult, assignAB),
                $"A={c.A}, B={c.B}: AB order should match explicit truth table.");
            Assert.AreEqual(c.Expected, managerBA.Evaluate(exprBAResult, assignBA),
                $"A={c.A}, B={c.B}: BA order should match explicit truth table.");
        }
    }
}
