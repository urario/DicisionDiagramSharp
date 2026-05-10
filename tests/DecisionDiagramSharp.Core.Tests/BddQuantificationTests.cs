using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddQuantificationTests
{
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
}
