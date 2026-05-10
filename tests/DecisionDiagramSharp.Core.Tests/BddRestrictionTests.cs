using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddRestrictionTests
{
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
}
