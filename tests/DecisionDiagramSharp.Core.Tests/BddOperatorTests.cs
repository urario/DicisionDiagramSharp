using System.Collections.Generic;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddOperatorTests
{
    [TestMethod]
    public void Operators_MatchBooleanTruthTableSemantics()
    {
        // Purpose: verifies that friendly BDD operators preserve Boolean semantics rather than merely invoking manager methods.
        // Arrange
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = (manager.Var(a) & !manager.Var(b)) | (manager.Var(a) ^ manager.Var(b));

        // Act / Assert
        for (var mask = 0; mask < 4; mask++)
        {
            var av = (mask & 1) != 0;
            var bv = (mask & 2) != 0;
            var assignment = new Dictionary<VariableId, bool>
            {
                { a, av },
                { b, bv }
            };
            var expected = (av && !bv) || (av ^ bv);
            Assert.AreEqual(expected, manager.Evaluate(expression, assignment));
        }
    }

    [TestMethod]
    public void Operators_RejectCrossManagerOperands()
    {
        // Purpose: confirms that operator overloads keep the manager-ownership safety boundary intact.
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

    [TestMethod]
    public void Operators_RejectDefaultHandle()
    {
        // Purpose: verifies that an uninitialized BDD handle reports a manager-ownership error instead of failing opaquely.
        // Arrange
        var value = default(Bdd);

        // Act / Assert
        Assert.Throws<DiagramManagerMismatchException>(() => !value);
    }
}
