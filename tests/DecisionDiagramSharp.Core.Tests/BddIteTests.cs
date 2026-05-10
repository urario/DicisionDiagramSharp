using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddIteTests
{
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
}
