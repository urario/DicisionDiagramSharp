using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddManagerTests
{
    // ─── A. Terminals ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that True and False terminals evaluate to their Boolean values.
    /// </summary>
    /// <remarks>
    ///Confirms terminal node semantics are correct before any non-terminal node tests depend on them.
    /// </remarks>
    [TestMethod]
    public void TerminalNodes_ShouldEvaluateToTheirBooleanValues()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var assignment = new Dictionary<VariableId, bool> { { a, true } };

        // Act / Assert
        Assert.IsTrue(manager.True.IsTrue);
        Assert.IsFalse(manager.True.IsFalse);
        Assert.IsTrue(manager.False.IsFalse);
        Assert.IsFalse(manager.False.IsTrue);
        Assert.IsTrue(manager.Evaluate(manager.True, assignment));
        Assert.IsFalse(manager.Evaluate(manager.False, assignment));
    }

    /// <summary>
    /// Verifies that Not(True) == False and Not(False) == True.
    /// </summary>
    /// <remarks>
    ///Guards the terminal cases of negation, which are prerequisites for all Boolean operation tests.
    /// </remarks>
    [TestMethod]
    public void Not_ShouldInvertTerminalNodes()
    {
        // Arrange
        var manager = new BddManager();

        // Act
        var notTrue = manager.Not(manager.True);
        var notFalse = manager.Not(manager.False);

        // Assert
        Assert.AreEqual(manager.False, notTrue);
        Assert.AreEqual(manager.True, notFalse);
    }

    /// <summary>
    /// Verifies that Var returns a satisfiable non-terminal node.
    /// </summary>
    /// <remarks>
    ///Confirms that single-variable nodes are distinguishable from terminals and correctly satisfiable.
    /// </remarks>
    [TestMethod]
    public void Var_ShouldReturnSatisfiableNonTerminalNode()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");

        // Act
        var aNode = manager.Var(a);
        var aNodeAgain = manager.Var("A");

        // Assert
        Assert.AreEqual(aNode, aNodeAgain);
        Assert.IsTrue(manager.IsSatisfiable(aNode));
        Assert.IsFalse(manager.IsSatisfiable(manager.False));
        Assert.AreEqual("A", manager.GetVariableName(a));
    }

    // ─── B. Identity Laws ─────────────────────────────────────────────────────

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

    // ─── C. De Morgan's Laws ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that !(A && B) == !A || !B (De Morgan's first law).
    /// </summary>
    /// <remarks>
    ///Confirms structural equivalence between Not-And and Or-of-Nots; verified by logical equivalence, not node identity.
    /// </remarks>
    [TestMethod]
    public void Not_And_ShouldBeEquivalentToOrOfNegatedOperands()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var notAnd = manager.Not(manager.And(aNode, bNode));
        var orOfNots = manager.Or(manager.Not(aNode), manager.Not(bNode));

        // Assert — logical equivalence (canonical node identity for ROBDD)
        Assert.AreEqual(notAnd, orOfNots, "!(A && B) must equal !A || !B as canonical nodes.");
    }

    /// <summary>
    /// Verifies that !(A || B) == !A && !B (De Morgan's second law).
    /// </summary>
    /// <remarks>
    ///Confirms structural equivalence between Not-Or and And-of-Nots; verified by canonical node identity.
    /// </remarks>
    [TestMethod]
    public void Not_Or_ShouldBeEquivalentToAndOfNegatedOperands()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var notOr = manager.Not(manager.Or(aNode, bNode));
        var andOfNots = manager.And(manager.Not(aNode), manager.Not(bNode));

        // Assert — canonical node identity
        Assert.AreEqual(notOr, andOfNots, "!(A || B) must equal !A && !B as canonical nodes.");
    }

    // ─── D. Implies / Equivalent ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that A => B == !A || B.
    /// </summary>
    /// <remarks>
    ///Confirms that Implies is correctly implemented as the material conditional, not a separate primitive.
    /// </remarks>
    [TestMethod]
    public void Implies_ShouldBeEquivalentToNotAOrB()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var implies = manager.Implies(aNode, bNode);
        var notAOrB = manager.Or(manager.Not(aNode), bNode);

        // Assert — canonical node identity
        Assert.AreEqual(implies, notAOrB, "A => B must equal !A || B as canonical nodes.");
    }

    /// <summary>
    /// Verifies that A <=> B == !(A ^ B).
    /// </summary>
    /// <remarks>
    ///Confirms that Equivalent is the negation of XOR, as required by Boolean equivalence semantics.
    /// </remarks>
    [TestMethod]
    public void Equivalent_ShouldBeEquivalentToNotXor()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var equiv = manager.Equivalent(aNode, bNode);
        var notXor = manager.Not(manager.Xor(aNode, bNode));

        // Assert — canonical node identity
        Assert.AreEqual(equiv, notXor, "A <=> B must equal !(A ^ B) as canonical nodes.");
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

    // ─── E. ITE ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Ite(True, X, Y) == X.
    /// </summary>
    /// <remarks>
    ///Guards the terminal case of ITE when condition is the True constant.
    /// </remarks>
    [TestMethod]
    public void Ite_WithTrueCondition_ShouldReturnThenBranch()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var result = manager.Ite(manager.True, aNode, bNode);

        // Assert
        Assert.AreEqual(aNode, result);
    }

    /// <summary>
    /// Verifies that Ite(False, X, Y) == Y.
    /// </summary>
    /// <remarks>
    ///Guards the terminal case of ITE when condition is the False constant.
    /// </remarks>
    [TestMethod]
    public void Ite_WithFalseCondition_ShouldReturnElseBranch()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);

        // Act
        var result = manager.Ite(manager.False, aNode, bNode);

        // Assert
        Assert.AreEqual(bNode, result);
    }

    /// <summary>
    /// Verifies that Restrict(Ite(A, B, !B), A=true) == B and Restrict(..., A=false) == !B.
    /// </summary>
    /// <remarks>
    ///Confirms that Restrict correctly selects the branch corresponding to the supplied condition variable assignment.
    /// </remarks>
    [TestMethod]
    public void Restrict_IteByConditionVariable_ShouldReturnSelectedBranch()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);
        var ite = manager.Ite(aNode, bNode, manager.Not(bNode));

        // Act
        var whenTrue = manager.Restrict(ite, a, true);
        var whenFalse = manager.Restrict(ite, a, false);

        // Assert
        Assert.AreEqual(bNode, whenTrue);
        Assert.AreEqual(manager.Not(bNode), whenFalse);
    }

    // ─── F. Quantification ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Exists(A, A && B) == B.
    /// </summary>
    /// <remarks>
    ///Confirms existential quantification over a conjunction removes the quantified variable correctly.
    /// </remarks>
    [TestMethod]
    public void Exists_OverConjunction_ShouldRemoveQuantifiedVariable()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Exists(expr, a);

        // Assert
        Assert.AreEqual(manager.Var(b), result);
    }

    /// <summary>
    /// Verifies that ForAll(A, A && B) == False.
    /// </summary>
    /// <remarks>
    ///Confirms universal quantification over a conjunction returns False when the quantified variable is required.
    /// </remarks>
    [TestMethod]
    public void ForAll_OverConjunction_ShouldRequireAllAssignments()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.ForAll(expr, a);

        // Assert
        Assert.AreEqual(manager.False, result);
    }

    /// <summary>
    /// Verifies that Exists(A, A || B) == True.
    /// </summary>
    /// <remarks>
    ///Confirms existential quantification over a disjunction returns True when the quantified variable satisfies the formula.
    /// </remarks>
    [TestMethod]
    public void Exists_OverDisjunction_ShouldReturnTrue()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Or(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Exists(expr, a);

        // Assert
        Assert.AreEqual(manager.True, result);
    }

    /// <summary>
    /// Verifies that ForAll(A, A || B) == B.
    /// </summary>
    /// <remarks>
    ///Confirms universal quantification over a disjunction eliminates the quantified variable while preserving the remaining operand.
    /// </remarks>
    [TestMethod]
    public void ForAll_OverDisjunction_ShouldReturnRemainingOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Or(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.ForAll(expr, a);

        // Assert
        Assert.AreEqual(manager.Var(b), result);
    }

    /// <summary>
    /// Verifies that Exists(A, A ^ B) == True when some assignment satisfies A ^ B.
    /// </summary>
    /// <remarks>
    ///Confirms existential quantification collapses to True when the formula is satisfiable under some assignment of the quantified variable.
    /// </remarks>
    [TestMethod]
    public void Exists_OverXor_ShouldReturnTrueWhenSomeAssignmentSatisfies()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Xor(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Exists(expr, a);

        // Assert
        Assert.AreEqual(manager.True, result);
    }

    /// <summary>
    /// Verifies that ForAll(A, A ^ B) == False when not all assignments of A satisfy A ^ B.
    /// </summary>
    /// <remarks>
    ///Confirms universal quantification collapses to False when XOR cannot be satisfied for all assignments of the quantified variable.
    /// </remarks>
    [TestMethod]
    public void ForAll_OverXor_ShouldReturnFalseWhenNotAllAssignmentsSatisfy()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Xor(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.ForAll(expr, a);

        // Assert
        Assert.AreEqual(manager.False, result);
    }

    // ─── G. Restrict ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Restrict(A && B, A=true) == B.
    /// </summary>
    /// <remarks>
    ///Confirms Restrict eliminates the fixed variable from a conjunction and returns the remaining operand.
    /// </remarks>
    [TestMethod]
    public void Restrict_AndByTrueVariable_ShouldReturnRemainingOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Restrict(expr, a, true);

        // Assert
        Assert.AreEqual(manager.Var(b), result);
    }

    /// <summary>
    /// Verifies that Restrict(A && B, A=false) == False.
    /// </summary>
    /// <remarks>
    ///Confirms Restrict returns False when the false assignment makes a conjunction unsatisfiable.
    /// </remarks>
    [TestMethod]
    public void Restrict_AndByFalseVariable_ShouldReturnFalse()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.And(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Restrict(expr, a, false);

        // Assert
        Assert.AreEqual(manager.False, result);
    }

    /// <summary>
    /// Verifies that Restrict(A || B, A=true) == True.
    /// </summary>
    /// <remarks>
    ///Confirms Restrict short-circuits to True when a true assignment satisfies the disjunction immediately.
    /// </remarks>
    [TestMethod]
    public void Restrict_OrByTrueVariable_ShouldReturnTrue()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Or(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Restrict(expr, a, true);

        // Assert
        Assert.AreEqual(manager.True, result);
    }

    /// <summary>
    /// Verifies that Restrict(A || B, A=false) == B.
    /// </summary>
    /// <remarks>
    ///Confirms Restrict eliminates the fixed variable from a disjunction and returns the remaining operand.
    /// </remarks>
    [TestMethod]
    public void Restrict_OrByFalseVariable_ShouldReturnRemainingOperand()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expr = manager.Or(manager.Var(a), manager.Var(b));

        // Act
        var result = manager.Restrict(expr, a, false);

        // Assert
        Assert.AreEqual(manager.Var(b), result);
    }

    // ─── H. Canonical Form ────────────────────────────────────────────────────

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
    ///Confirms ITE with trivial branches reduces to the condition node itself, following ROBDD reduction rules.
    /// </remarks>
    [TestMethod]
    public void Ite_WithSameBranches_ShouldReturnBranch()
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

    // ─── I. Variable Ordering ─────────────────────────────────────────────────

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

    // ─── J. Extra / Unused Variables ─────────────────────────────────────────

    /// <summary>
    /// Verifies that EnumerateModels expands variables not used in the expression.
    /// </summary>
    /// <remarks>
    ///Confirms that don't-care variables are included in every enumerated model with both true and false assignments,
    /// and that the total model count reflects the expansion.
    /// </remarks>
    [TestMethod]
    public void EnumerateModels_ShouldExpandVariablesNotUsedInExpression()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        // Act
        var models = manager.EnumerateModels(expression, new ModelEnumerationOptions { MaxModels = 10 });

        // Assert — C is don't-care: 2 models expected (A=true, B=false, C=false) and (A=true, B=false, C=true)
        Assert.HasCount(2, models);
        foreach (var model in models)
        {
            Assert.IsTrue(model[a], "A must be true in every model.");
            Assert.IsFalse(model[b], "B must be false in every model.");
            Assert.IsTrue(model.ContainsKey(c), "C (don't-care) must be present in every model.");
        }

        var cValues = new HashSet<bool> { models[0][c], models[1][c] };
        Assert.IsTrue(cValues.Contains(true) && cValues.Contains(false),
            "Both C=true and C=false must appear across the enumerated models.");
    }

    /// <summary>
    /// Verifies that Evaluate throws when a required variable is missing from the assignment.
    /// </summary>
    /// <remarks>
    ///Guards the API contract that every variable in the expression must have an assignment; missing entries are an error.
    /// </remarks>
    [TestMethod]
    public void Evaluate_ShouldThrowWhenRequiredVariableIsMissing()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Var(b));
        var incompleteAssignment = new Dictionary<VariableId, bool> { { a, true } };

        // Act / Assert
        Assert.Throws<ArgumentException>(
            () => manager.Evaluate(expression, incompleteAssignment));
    }

    // ─── K. Boundary Values and Error Cases ──────────────────────────────────

    /// <summary>
    /// Verifies that EnumerateModels returns all models when count equals MaxModels.
    /// </summary>
    /// <remarks>
    ///Confirms the at-exactly-the-limit case does not throw; the limit is inclusive.
    /// Var(a) with two registered variables (A and B) expands B over both values:
    /// models = {A=true,B=false} and {A=true,B=true} → exactly 2 models.
    /// </remarks>
    [TestMethod]
    public void EnumerateModels_ShouldReturnAllModelsWhenCountEqualsMaxModels()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");

        // Act — Var(a) with B also registered yields 2 models; MaxModels = 2 is the exact limit
        var models = manager.EnumerateModels(manager.Var(a), new ModelEnumerationOptions { MaxModels = 2 });

        // Assert
        Assert.HasCount(2, models);
    }

    /// <summary>
    /// Verifies that EnumerateModels throws when model count exceeds MaxModels.
    /// </summary>
    /// <remarks>
    ///Guards the safety limit that prevents unbounded enumeration from consuming excessive memory.
    /// Var(a) with two registered variables (A and B) yields 2 models; MaxModels = 1 is exceeded.
    /// </remarks>
    [TestMethod]
    public void EnumerateModels_ShouldThrowWhenResultCountExceedsMaxModels()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        manager.GetOrAddVariable("B");

        // Act / Assert — 2 models exceed the limit of 1
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => manager.EnumerateModels(manager.Var(a), new ModelEnumerationOptions { MaxModels = 1 }));
    }

    /// <summary>
    /// Verifies that EnumerateModels throws when MaxModels is zero.
    /// </summary>
    /// <remarks>
    ///Guards the API contract that MaxModels must be a positive integer; zero is not a valid limit.
    /// </remarks>
    [TestMethod]
    public void EnumerateModels_ShouldThrowWhenMaxModelsIsZero()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => manager.EnumerateModels(manager.Var(a), new ModelEnumerationOptions { MaxModels = 0 }));
    }

    /// <summary>
    /// Verifies that binary operations throw when operands come from different managers.
    /// </summary>
    /// <remarks>
    ///Guards manager ownership isolation; mixing handles from different managers must be a detectable error.
    /// </remarks>
    [TestMethod]
    public void BinaryOperation_ShouldThrowWhenManagersDoNotMatch()
    {
        // Arrange
        var left = new BddManager();
        var right = new BddManager();
        var leftNode = left.Var(left.GetOrAddVariable("A"));
        var rightNode = right.Var(right.GetOrAddVariable("B"));

        // Act / Assert
        Assert.Throws<DiagramManagerMismatchException>(() => left.And(leftNode, rightNode));
    }

    /// <summary>
    /// Verifies that Var throws ArgumentNullException when variable name is null.
    /// </summary>
    /// <remarks>
    ///Guards the null-name API contract; callers must not pass null as a variable name.
    /// </remarks>
    [TestMethod]
    public void Var_ShouldThrowWhenVariableIdIsNull()
    {
        // Arrange
        var manager = new BddManager();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => manager.Var(null!));
    }

    /// <summary>
    /// Verifies that Var throws ArgumentOutOfRangeException when VariableId is unknown to this manager.
    /// </summary>
    /// <remarks>
    ///Guards the ownership contract that VariableId values must be registered in the manager before use.
    /// </remarks>
    [TestMethod]
    public void Var_ShouldThrowWhenVariableIdIsUnknown()
    {
        // Arrange
        var manager = new BddManager();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.Var(new VariableId(99)));
    }

    /// <summary>
    /// Verifies that Evaluate throws ArgumentNullException when the assignment dictionary is null.
    /// </summary>
    /// <remarks>
    ///Guards the null-assignment API contract; callers must supply a non-null dictionary.
    /// </remarks>
    [TestMethod]
    public void Evaluate_ShouldThrowWhenAssignmentIsNull()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var node = manager.Var(a);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => manager.Evaluate(node, (IReadOnlyDictionary<VariableId, bool>)null!));
    }

    /// <summary>
    /// Verifies that node creation throws DiagramSizeLimitExceededException when the node count exceeds MaxNodeCount.
    /// </summary>
    /// <remarks>
    ///Guards the size limit contract; operations must fail with an actionable exception before the memory budget is exhausted.
    /// </remarks>
    [TestMethod]
    public void SizeLimit_ShouldThrowWhenNodeCountExceedsMaximum()
    {
        // Arrange
        var limited = new BddManager(new DecisionDiagramOptions { MaxNodeCount = 1 });
        var la = limited.GetOrAddVariable("A");
        var lb = limited.GetOrAddVariable("B");

        // Act / Assert
        Assert.Throws<DiagramSizeLimitExceededException>(
            () => limited.And(limited.Var(la), limited.Var(lb)));
    }

    // ─── Internal / Coverage-Driven ──────────────────────────────────────────

    /// <summary>
    /// Verifies that Validate detects corrupted internal node state.
    /// </summary>
    /// <remarks>
    ///Covers internal invariant checks (equal children, out-of-range references, variable ordering, unique table)
    /// that protect canonicalization correctness. These tests intentionally corrupt internal state via reflection.
    /// </remarks>
    [TestMethod]
    public void InternalValidation_ShouldDetectCorruptedNodeState()
    {
        // Arrange / Act / Assert — equal children
        var managerEqualChildren = CreateManagerWithTwoVariableConjunction(out var a1, out _);
        SetNode(managerEqualChildren, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerEqualChildren.Validate());

        // Arrange / Act / Assert — out-of-range child
        var managerOutOfRange = CreateManagerWithTwoVariableConjunction(out var a2, out _);
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation
        var managerOrdering = CreateManagerWithTwoVariableConjunction(out var a3, out _);
        SetNode(managerOrdering, 0, CreateNode(a3.Value, 2, 1));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        // Arrange / Act / Assert — unique table cleared
        var managerUnique = CreateManagerWithTwoVariableConjunction(out _, out _);
        var uniqueTable = (IDictionary)TestHelpers.GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    /// <summary>
    /// Verifies that private key types (BddKey, IteKey, CountKey) implement value equality correctly.
    /// </summary>
    /// <remarks>
    ///Covers internal unique-table and cache key semantics; incorrect equality would break canonicalization and caching.
    /// </remarks>
    [TestMethod]
    public void InternalKeyTypes_ShouldImplementValueEquality()
    {
        // Arrange / Act / Assert — BddKey
        var bddKeyType = typeof(BddManager).GetNestedType("BddKey", BindingFlags.NonPublic)!;
        var bddKey1 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey2 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey3 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        AssertPrivateObjectEquals(bddKeyType, bddKey1, bddKey2, bddKey3);

        // Arrange / Act / Assert — IteKey
        var iteKeyType = typeof(BddManager).GetNestedType("IteKey", BindingFlags.NonPublic)!;
        var iteKey1 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey2 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey3 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 3, 1, 0 }, null)!;
        AssertPrivateObjectEquals(iteKeyType, iteKey1, iteKey2, iteKey3);

        // Arrange / Act / Assert — CountKey
        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var countKey1 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey2 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey3 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 3 }, null)!;
        AssertPrivateObjectEquals(countKeyType, countKey1, countKey2, countKey3);
    }

    /// <summary>
    /// Verifies that private recursive helpers (CountModelsNode, EnumerateModelsRecursive, IsReachable) cover rare branches.
    /// </summary>
    /// <remarks>
    ///Covers memoization re-entry and limit enforcement inside private recursive helpers that are unreachable from the public API in isolation.
    /// </remarks>
    [TestMethod]
    public void InternalHelpers_ShouldCoverEdgeCaseBranches()
    {
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var expression = manager.Var(a);

        var countModelsNode = typeof(BddManager).GetMethod("CountModelsNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var memoType = typeof(Dictionary<,>).MakeGenericType(countKeyType, typeof(long));
        var memo = Activator.CreateInstance(memoType)!;
        var expressionNodeId = GetBddNodeId(expression);

        // Act — first call populates memo; second call hits the memoized branch
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));

        // Act / Assert — EnumerateModelsRecursive throws when seed result already exceeds limit
        var enumerateModelsRecursive = typeof(BddManager).GetMethod("EnumerateModelsRecursive", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = Assert.Throws<TargetInvocationException>(() =>
            enumerateModelsRecursive.Invoke(
                manager,
                new object[]
                {
                    1,
                    1,
                    new bool[manager.VariableCount],
                    new List<IReadOnlyDictionary<VariableId, bool>> { new Dictionary<VariableId, bool>() },
                    0
                }));
        Assert.IsInstanceOfType(target.InnerException, typeof(DiagramEnumerationLimitExceededException));

        // Act / Assert — IsReachable returns false for a node outside the diagram
        var isReachable = typeof(BddManager).GetMethod("IsReachable", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.IsFalse((bool)isReachable.Invoke(manager, new object[] { expressionNodeId, 999 })!);
    }

    /// <summary>
    /// Verifies that randomized Boolean operations match naive truth tables for all combinations of 4 variables.
    /// </summary>
    /// <remarks>
    ///Provides broad behavioral coverage for Not, And, Or, Xor, Ite, Implies, and Equivalent
    /// over 100 randomized inputs; catches systematic errors in the ITE-based algorithm.
    /// </remarks>
    [TestMethod]
    public void RandomizedOperations_ShouldMatchNaiveTruthTables()
    {
        // Arrange
        var random = new Random(20260507);

        for (var iteration = 0; iteration < 100; iteration++)
        {
            var manager = new BddManager();
            var variables = new[]
            {
                manager.GetOrAddVariable("A"),
                manager.GetOrAddVariable("B"),
                manager.GetOrAddVariable("C"),
                manager.GetOrAddVariable("D")
            };
            var leaves = new Bdd[variables.Length];
            for (var i = 0; i < variables.Length; i++)
            {
                leaves[i] = manager.Var(variables[i]);
            }

            var leftIndex = random.Next(leaves.Length);
            var rightIndex = random.Next(leaves.Length);
            var op = random.Next(7);
            var expression = BuildExpression(manager, leaves[leftIndex], leaves[rightIndex], op);

            for (var mask = 0; mask < 16; mask++)
            {
                // Act
                var assignment = TestHelpers.BuildBoolAssignment(variables, mask);
                var lv = assignment[variables[leftIndex]];
                var rv = assignment[variables[rightIndex]];

                // Assert
                Assert.AreEqual(EvaluateNaive(lv, rv, op), manager.Evaluate(expression, assignment),
                    $"iteration={iteration}, op={op}, mask={mask}: BDD result should match naive truth table.");
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Bdd BuildExpression(BddManager manager, Bdd left, Bdd right, int op)
    {
        switch (op)
        {
            case 0: return manager.Not(left);
            case 1: return manager.And(left, right);
            case 2: return manager.Or(left, right);
            case 3: return manager.Xor(left, right);
            case 4: return manager.Ite(left, right, manager.False);
            case 5: return manager.Implies(left, right);
            default: return manager.Equivalent(left, right);
        }
    }

    private static bool EvaluateNaive(bool left, bool right, int op)
    {
        switch (op)
        {
            case 0: return !left;
            case 1: return left && right;
            case 2: return left || right;
            case 3: return left ^ right;
            case 4: return left ? right : false;
            case 5: return !left || right;
            default: return left == right;
        }
    }

    private static BddManager CreateManagerWithTwoVariableConjunction(out VariableId a, out VariableId b)
    {
        var manager = new BddManager();
        a = manager.GetOrAddVariable("A");
        b = manager.GetOrAddVariable("B");
        _ = manager.And(manager.Var(a), manager.Var(b));
        return manager;
    }

    private static void SetNode(BddManager manager, int index, object node)
    {
        var nodes = (IList)TestHelpers.GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(BddManager).GetNestedType("BddNode", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }

    private static int GetBddNodeId(Bdd value)
    {
        var property = typeof(Bdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void AssertPrivateObjectEquals(Type type, object first, object second, object third)
    {
        var equalsObject = type.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode());
    }

    // ─── L. String-name helpers (V07) ────────────────────────────────────────

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
