using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddModelEnumerationTests
{
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
}
