using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddErrorContractTests
{
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
    /// Verifies that Var(string) throws ArgumentNullException when the string name is null.
    /// </summary>
    /// <remarks>
    ///Guards the null string-name API contract; VariableId is a struct and cannot be null,
    /// so this test targets the string-name overload Var(string name).
    /// </remarks>
    [TestMethod]
    public void Var_StringNameNull_ShouldThrowArgumentNullException()
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
    public void Bdd_SizeLimit_ShouldThrowWhenNodeCountExceedsMaximum()
    {
        // Arrange
        var limited = new BddManager(new DecisionDiagramOptions { MaxNodeCount = 1 });
        var la = limited.GetOrAddVariable("A");
        var lb = limited.GetOrAddVariable("B");

        // Act / Assert
        Assert.Throws<DiagramSizeLimitExceededException>(
            () => limited.And(limited.Var(la), limited.Var(lb)));
    }
}
