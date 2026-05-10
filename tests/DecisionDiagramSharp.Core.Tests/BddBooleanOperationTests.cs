using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddBooleanOperationTests
{
    // ─── Identity Laws ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that A && A == A (And is idempotent).
    /// </summary>
    /// <remarks>
    ///Guards the canonicalization invariant: identical operands must reduce to the same canonical node.
    /// </remarks>
    [TestMethod]
    public void And_ShouldBeIdempotent()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.And(aNode, aNode);

        // Assert
        Assert.AreEqual(aNode, result);
    }

    /// <summary>
    /// Verifies that A || A == A (Or is idempotent).
    /// </summary>
    /// <remarks>
    ///Guards the canonicalization invariant for Or: identical operands must reduce to the same canonical node.
    /// </remarks>
    [TestMethod]
    public void Or_ShouldBeIdempotent()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Or(aNode, aNode);

        // Assert
        Assert.AreEqual(aNode, result);
    }

    /// <summary>
    /// Verifies that A && False == False.
    /// </summary>
    /// <remarks>
    ///Guards the annihilator law for conjunction; False is the absorbing element for And.
    /// </remarks>
    [TestMethod]
    public void And_WithFalse_ShouldReturnFalse()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.And(aNode, manager.False);

        // Assert
        Assert.AreEqual(manager.False, result);
    }

    /// <summary>
    /// Verifies that A || True == True.
    /// </summary>
    /// <remarks>
    ///Guards the annihilator law for disjunction; True is the absorbing element for Or.
    /// </remarks>
    [TestMethod]
    public void Or_WithTrue_ShouldReturnTrue()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Or(aNode, manager.True);

        // Assert
        Assert.AreEqual(manager.True, result);
    }

    /// <summary>
    /// Verifies that A && True == A.
    /// </summary>
    /// <remarks>
    ///Guards the identity law for And; True is the identity element.
    /// </remarks>
    [TestMethod]
    public void And_WithTrue_ShouldReturnOriginalOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.And(aNode, manager.True);

        // Assert
        Assert.AreEqual(aNode, result);
    }

    /// <summary>
    /// Verifies that A || False == A.
    /// </summary>
    /// <remarks>
    ///Guards the identity law for Or; False is the identity element.
    /// </remarks>
    [TestMethod]
    public void Or_WithFalse_ShouldReturnOriginalOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Or(aNode, manager.False);

        // Assert
        Assert.AreEqual(aNode, result);
    }

    /// <summary>
    /// Verifies that A ^ A == False.
    /// </summary>
    /// <remarks>
    ///Guards the self-cancellation property of XOR; used as a building block for equivalence checking.
    /// </remarks>
    [TestMethod]
    public void Xor_WithSelf_ShouldReturnFalse()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Xor(aNode, aNode);

        // Assert
        Assert.AreEqual(manager.False, result);
    }

    /// <summary>
    /// Verifies that !!A == A (double negation cancels out).
    /// </summary>
    /// <remarks>
    ///Guards the involution property of Not; regression test for complement path correctness.
    /// </remarks>
    [TestMethod]
    public void Not_Not_ShouldReturnOriginalExpression()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Not(manager.Not(expr));

        // Assert
        Assert.AreEqual(expr, result);
    }

    // ─── De Morgan's Laws ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that !(A && B) evaluates to the explicit truth table for De Morgan's first law.
    /// </summary>
    /// <remarks>
    /// Truth table: A=0,B=0→1; A=0,B=1→1; A=1,B=0→1; A=1,B=1→0.
    /// Uses an independent truth-table oracle rather than a BDD-operation-built oracle.
    /// </remarks>
    [TestMethod]
    public void Not_And_ShouldMatchDeMorganFirstLawTruthTable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var notAnd = manager.Not(manager.And(manager.Var(a), manager.Var(b)));
        // Truth table for !(A && B): false only when both are true
        var expected = new[] { true, true, true, false };

        // Act / Assert
        for (var mask = 0; mask < 4; mask++)
        {
            var assignment = TestHelpers.BuildBoolAssignment(new[] { a, b }, mask);
            Assert.AreEqual(expected[mask], manager.Evaluate(notAnd, assignment),
                $"mask={mask}: !(A && B) truth-table mismatch.");
        }
    }

    /// <summary>
    /// Verifies that !(A || B) evaluates to the explicit truth table for De Morgan's second law.
    /// </summary>
    /// <remarks>
    /// Truth table: A=0,B=0→1; A=0,B=1→0; A=1,B=0→0; A=1,B=1→0.
    /// Uses an independent truth-table oracle rather than a BDD-operation-built oracle.
    /// </remarks>
    [TestMethod]
    public void Not_Or_ShouldMatchDeMorganSecondLawTruthTable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var notOr = manager.Not(manager.Or(manager.Var(a), manager.Var(b)));
        // Truth table for !(A || B): true only when both are false
        var expected = new[] { true, false, false, false };

        // Act / Assert
        for (var mask = 0; mask < 4; mask++)
        {
            var assignment = TestHelpers.BuildBoolAssignment(new[] { a, b }, mask);
            Assert.AreEqual(expected[mask], manager.Evaluate(notOr, assignment),
                $"mask={mask}: !(A || B) truth-table mismatch.");
        }
    }

    // ─── Implies / Equivalent ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that A => B evaluates to the explicit truth table for material implication.
    /// </summary>
    /// <remarks>
    /// Truth table: A=0,B=0→1; A=0,B=1→1; A=1,B=0→0; A=1,B=1→1.
    /// Uses an independent truth-table oracle rather than a BDD-operation-built oracle.
    /// </remarks>
    [TestMethod]
    public void Implies_ShouldMatchMaterialImplicationTruthTable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var implies = manager.Implies(manager.Var(a), manager.Var(b));
        // Truth table for A => B (mask bit0=A, bit1=B): false only when A=true, B=false (mask=1)
        var expected = new[] { true, false, true, true };

        // Act / Assert
        for (var mask = 0; mask < 4; mask++)
        {
            var assignment = TestHelpers.BuildBoolAssignment(new[] { a, b }, mask);
            Assert.AreEqual(expected[mask], manager.Evaluate(implies, assignment),
                $"mask={mask}: A => B truth-table mismatch.");
        }
    }

    /// <summary>
    /// Verifies that A &lt;=&gt; B evaluates to the explicit truth table for Boolean equivalence.
    /// </summary>
    /// <remarks>
    /// Truth table: A=0,B=0→1; A=0,B=1→0; A=1,B=0→0; A=1,B=1→1.
    /// Uses an independent truth-table oracle rather than a BDD-operation-built oracle.
    /// </remarks>
    [TestMethod]
    public void Equivalent_ShouldMatchBooleanEquivalenceTruthTable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var equiv = manager.Equivalent(manager.Var(a), manager.Var(b));
        // Truth table for A <=> B: true only when A and B have the same value
        var expected = new[] { true, false, false, true };

        // Act / Assert
        for (var mask = 0; mask < 4; mask++)
        {
            var assignment = TestHelpers.BuildBoolAssignment(new[] { a, b }, mask);
            Assert.AreEqual(expected[mask], manager.Evaluate(equiv, assignment),
                $"mask={mask}: A <=> B truth-table mismatch.");
        }
    }

    /// <summary>
    /// Verifies that A => A == True (self-implication is a tautology).
    /// </summary>
    /// <remarks>
    ///Guards the reflexivity of implication; the result must reduce to the True terminal.
    /// </remarks>
    [TestMethod]
    public void Implies_SameExpression_ShouldReturnTrue()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Implies(aNode, aNode);

        // Assert
        Assert.AreEqual(manager.True, result);
    }

    /// <summary>
    /// Verifies that A <=> A == True (self-equivalence is a tautology).
    /// </summary>
    /// <remarks>
    ///Guards the reflexivity of equivalence; verifies canonical reduction to True terminal.
    /// </remarks>
    [TestMethod]
    public void Equivalent_SameExpression_ShouldReturnTrue()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var aNode = manager.Var(a);

        // Act
        var result = manager.Equivalent(aNode, aNode);

        // Assert
        Assert.AreEqual(manager.True, result);
    }
}
