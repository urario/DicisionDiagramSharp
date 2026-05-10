using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddTerminalTests
{
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
}
