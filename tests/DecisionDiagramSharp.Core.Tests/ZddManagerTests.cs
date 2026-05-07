using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class ZddManagerTests
{
    [TestMethod]
    public void VariableTable_IsDeterministic()
    {
        var table = new VariableTable();
        var a1 = table.GetOrAdd("A");
        var a2 = table.GetOrAdd("A");
        var b = table.GetOrAdd("B");

        Assert.AreEqual(a1, a2);
        Assert.AreNotEqual(a1, b);
        Assert.AreEqual("A", table.GetName(a1));
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Terminals_HaveExpectedCounts()
    {
        var manager = new ZddManager();
        Assert.AreEqual(0L, manager.CountSets(manager.Empty));
        Assert.AreEqual(1L, manager.CountSets(manager.Base));
    }

    [TestMethod]
    public void MakeSet_RoundTripsThroughEnumeration()
    {
        var manager = CreateManagerWithVariables("A", "B", "C");
        var set = manager.MakeSet(new[] { Var(manager, "C"), Var(manager, "A"), Var(manager, "A") });
        var rows = manager.EnumerateSets(set);
        Assert.HasCount(1, rows);
        CollectionAssert.AreEqual(new[] { "A", "C" }, ToNames(manager, rows[0]).ToArray());
    }

    [TestMethod]
    public void Subset1_And_Containing_AreDifferent()
    {
        var manager = CreateManagerWithVariables("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { a, b },
                new[] { a }
            });

        var subset1 = manager.Subset1(family, a);
        var containing = manager.Containing(family, a);
        var subset1AsNames = EnumeratedKeys(manager, subset1);
        var containingAsNames = EnumeratedKeys(manager, containing);

        CollectionAssert.AreEquivalent(new[] { "", "B" }, subset1AsNames);
        CollectionAssert.AreEquivalent(new[] { "A", "A,B" }, containingAsNames);
    }

    [TestMethod]
    public void NotContaining_And_Change_Work()
    {
        var manager = CreateManagerWithVariables("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { a, b },
                new[] { b }
            });

        var notContainingA = manager.NotContaining(family, a);
        CollectionAssert.AreEquivalent(new[] { "B" }, EnumeratedKeys(manager, notContainingA));

        var toggledA = manager.Change(notContainingA, a);
        CollectionAssert.AreEquivalent(new[] { "A,B" }, EnumeratedKeys(manager, toggledA));
    }

    [TestMethod]
    public void EnumerateSets_RespectsLimit()
    {
        var manager = CreateManagerWithVariables("A", "B", "C");
        var family = manager.MakeFamily(
            new[]
            {
                new[] { Var(manager, "A") },
                new[] { Var(manager, "B") },
                new[] { Var(manager, "C") }
            });

        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => manager.EnumerateSets(family, new SetEnumerationOptions { MaxSets = 2 }));
    }

    [TestMethod]
    public void ManagerMismatch_ThrowsActionableException()
    {
        var leftManager = CreateManagerWithVariables("A");
        var rightManager = CreateManagerWithVariables("A");
        var left = leftManager.MakeSet(new[] { Var(leftManager, "A") });
        var right = rightManager.MakeSet(new[] { Var(rightManager, "A") });

        var ex = Assert.Throws<DiagramManagerMismatchException>(() => leftManager.Union(left, right));
        StringAssert.Contains(ex.Message, "different ZddManager");
    }

    [TestMethod]
    public void SizeLimit_Throws()
    {
        var manager = new ZddManager(new DecisionDiagramOptions { MaxNodeCount = 1 });
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");
        Assert.Throws<DiagramSizeLimitExceededException>(
            () => manager.MakeFamily(
                new[]
                {
                    new[] { a },
                    new[] { b },
                    new[] { c }
                }));
    }

    [TestMethod]
    public void Randomized_OperationsMatchNaiveModel()
    {
        var seed = 12345;
        var random = new Random(seed);
        for (var iteration = 0; iteration < 150; iteration++)
        {
            var manager = CreateManagerWithVariables("A", "B", "C", "D", "E", "F");
            var variableIds = new[]
            {
                Var(manager, "A"),
                Var(manager, "B"),
                Var(manager, "C"),
                Var(manager, "D"),
                Var(manager, "E"),
                Var(manager, "F")
            };

            var leftNaive = CreateRandomNaiveFamily(random, variableIds, 20);
            var rightNaive = CreateRandomNaiveFamily(random, variableIds, 20);
            var pivot = variableIds[random.Next(variableIds.Length)];

            var left = manager.MakeFamily(ToSets(leftNaive, variableIds));
            var right = manager.MakeFamily(ToSets(rightNaive, variableIds));

            AssertSetsEqual(UnionNaive(leftNaive, rightNaive), EnumeratedNaive(manager, manager.Union(left, right)));
            AssertSetsEqual(IntersectNaive(leftNaive, rightNaive), EnumeratedNaive(manager, manager.Intersect(left, right)));
            AssertSetsEqual(DifferenceNaive(leftNaive, rightNaive), EnumeratedNaive(manager, manager.Difference(left, right)));
            AssertSetsEqual(Subset0Naive(leftNaive, pivot.Value), EnumeratedNaive(manager, manager.Subset0(left, pivot)));
            AssertSetsEqual(Subset1Naive(leftNaive, pivot.Value), EnumeratedNaive(manager, manager.Subset1(left, pivot)));
            AssertSetsEqual(ContainingNaive(leftNaive, pivot.Value), EnumeratedNaive(manager, manager.Containing(left, pivot)));
            AssertSetsEqual(NotContainingNaive(leftNaive, pivot.Value), EnumeratedNaive(manager, manager.NotContaining(left, pivot)));
            AssertSetsEqual(ChangeNaive(leftNaive, pivot.Value), EnumeratedNaive(manager, manager.Change(left, pivot)));
            Assert.AreEqual(leftNaive.Count, manager.CountSets(left));
        }
    }

    private static ZddManager CreateManagerWithVariables(params string[] names)
    {
        var manager = new ZddManager();
        for (var i = 0; i < names.Length; i++)
        {
            manager.GetOrAddVariable(names[i]);
        }

        return manager;
    }

    private static VariableId Var(ZddManager manager, string name)
    {
        return manager.GetOrAddVariable(name);
    }

    private static IEnumerable<string> ToNames(ZddManager manager, IReadOnlyList<VariableId> set)
    {
        for (var i = 0; i < set.Count; i++)
        {
            yield return manager.GetVariableName(set[i]);
        }
    }

    private static string[] EnumeratedKeys(ZddManager manager, Zdd value)
    {
        var sets = manager.EnumerateSets(value);
        var keys = new string[sets.Count];
        for (var i = 0; i < sets.Count; i++)
        {
            keys[i] = string.Join(",", ToNames(manager, sets[i]));
        }

        return keys;
    }

    private static HashSet<string> CreateRandomNaiveFamily(Random random, IReadOnlyList<VariableId> vars, int maxSetCount)
    {
        var family = new HashSet<string>(StringComparer.Ordinal);
        var count = random.Next(maxSetCount + 1);
        for (var i = 0; i < count; i++)
        {
            var selected = new List<int>();
            for (var v = 0; v < vars.Count; v++)
            {
                if (random.NextDouble() < 0.35d)
                {
                    selected.Add(vars[v].Value);
                }
            }

            selected.Sort();
            family.Add(string.Join(",", selected));
        }

        return family;
    }

    private static IEnumerable<IEnumerable<VariableId>> ToSets(HashSet<string> naive, IReadOnlyList<VariableId> vars)
    {
        foreach (var key in naive)
        {
            if (key.Length == 0)
            {
                yield return Array.Empty<VariableId>();
                continue;
            }

            var parts = key.Split(',');
            var list = new List<VariableId>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var id = int.Parse(parts[i]);
                for (var j = 0; j < vars.Count; j++)
                {
                    if (vars[j].Value == id)
                    {
                        list.Add(vars[j]);
                        break;
                    }
                }
            }

            yield return list;
        }
    }

    private static HashSet<string> EnumeratedNaive(ZddManager manager, Zdd value)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var sets = manager.EnumerateSets(value, new SetEnumerationOptions { MaxSets = 10000 });
        for (var i = 0; i < sets.Count; i++)
        {
            var set = sets[i];
            var ids = new List<int>(set.Count);
            for (var j = 0; j < set.Count; j++)
            {
                ids.Add(set[j].Value);
            }

            ids.Sort();
            result.Add(string.Join(",", ids));
        }

        return result;
    }

    private static HashSet<string> UnionNaive(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.UnionWith(right);
        return output;
    }

    private static HashSet<string> IntersectNaive(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.IntersectWith(right);
        return output;
    }

    private static HashSet<string> DifferenceNaive(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.ExceptWith(right);
        return output;
    }

    private static HashSet<string> Subset0Naive(HashSet<string> family, int variable)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in family)
        {
            var values = ParseSet(set);
            if (!values.Contains(variable))
            {
                output.Add(set);
            }
        }

        return output;
    }

    private static HashSet<string> Subset1Naive(HashSet<string> family, int variable)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in family)
        {
            var values = ParseSet(set);
            if (values.Remove(variable))
            {
                output.Add(string.Join(",", values.OrderBy(x => x)));
            }
        }

        return output;
    }

    private static HashSet<string> ContainingNaive(HashSet<string> family, int variable)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in family)
        {
            if (ParseSet(set).Contains(variable))
            {
                output.Add(set);
            }
        }

        return output;
    }

    private static HashSet<string> NotContainingNaive(HashSet<string> family, int variable)
    {
        return Subset0Naive(family, variable);
    }

    private static HashSet<string> ChangeNaive(HashSet<string> family, int variable)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in family)
        {
            var values = ParseSet(set);
            if (!values.Add(variable))
            {
                values.Remove(variable);
            }

            output.Add(string.Join(",", values.OrderBy(x => x)));
        }

        return output;
    }

    private static HashSet<int> ParseSet(string key)
    {
        var values = new HashSet<int>();
        if (string.IsNullOrEmpty(key))
        {
            return values;
        }

        var parts = key.Split(',');
        for (var i = 0; i < parts.Length; i++)
        {
            values.Add(int.Parse(parts[i]));
        }

        return values;
    }

    private static void AssertSetsEqual(HashSet<string> expected, HashSet<string> actual)
    {
        CollectionAssert.AreEquivalent(expected.ToList(), actual.ToList());
    }
}
