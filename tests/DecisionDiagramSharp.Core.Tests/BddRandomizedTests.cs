using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddRandomizedTests
{
    /// <summary>
    /// Verifies that compound BDD expression trees match an independent naive evaluator for all variable assignments.
    /// </summary>
    /// <remarks>
    /// Builds random 3-level expression trees over 4 variables and verifies every assignment (all 16 masks)
    /// against an independent expression-tree evaluator that shares no code with BddManager.
    /// Seed 20260507, 100 iterations; failure messages include iteration, tree, and mask.
    /// </remarks>
    [TestMethod]
    public void RandomizedCompoundExpressions_ShouldMatchIndependentEvaluator()
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

            // Build a random 3-level compound tree and a parallel independent evaluator
            var (bddExpr, naiveExpr) = BuildRandomTree(manager, leaves, random, depth: 3);

            for (var mask = 0; mask < 16; mask++)
            {
                // Act
                var assignment = TestHelpers.BuildBoolAssignment(variables, mask);
                var bits = new bool[variables.Length];
                for (var i = 0; i < variables.Length; i++)
                {
                    bits[i] = assignment[variables[i]];
                }

                var bddResult = manager.Evaluate(bddExpr, assignment);
                var naiveResult = naiveExpr(bits);

                // Assert
                Assert.AreEqual(naiveResult, bddResult,
                    $"seed=20260507, iteration={iteration}, mask={mask}: compound BDD tree must match independent evaluator.");
            }
        }
    }

    private static (Bdd bdd, Func<bool[], bool> naive) BuildRandomTree(
        BddManager manager, Bdd[] leaves, Random random, int depth)
    {
        if (depth == 0)
        {
            var idx = random.Next(leaves.Length);
            var capturedIdx = idx;
            return (leaves[idx], bits => bits[capturedIdx]);
        }

        var op = random.Next(6);
        if (op == 0)
        {
            var (childBdd, childNaive) = BuildRandomTree(manager, leaves, random, depth - 1);
            return (manager.Not(childBdd), bits => !childNaive(bits));
        }

        var (leftBdd, leftNaive) = BuildRandomTree(manager, leaves, random, depth - 1);
        var (rightBdd, rightNaive) = BuildRandomTree(manager, leaves, random, depth - 1);

        return op switch
        {
            1 => (manager.And(leftBdd, rightBdd), bits => leftNaive(bits) && rightNaive(bits)),
            2 => (manager.Or(leftBdd, rightBdd), bits => leftNaive(bits) || rightNaive(bits)),
            3 => (manager.Xor(leftBdd, rightBdd), bits => leftNaive(bits) ^ rightNaive(bits)),
            4 => (manager.Implies(leftBdd, rightBdd), bits => !leftNaive(bits) || rightNaive(bits)),
            _ => (manager.Equivalent(leftBdd, rightBdd), bits => leftNaive(bits) == rightNaive(bits)),
        };
    }
}
