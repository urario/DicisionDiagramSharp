using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

internal static class ZddNaiveModel
{
    public static HashSet<string> CreateRandom(Random random, IReadOnlyList<VariableId> vars, int maxSetCount)
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

    public static IEnumerable<IEnumerable<VariableId>> ToSets(HashSet<string> naive, IReadOnlyList<VariableId> vars)
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

    public static HashSet<string> Enumerate(ZddManager manager, Zdd value, int maxSets = 10000)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var sets = manager.EnumerateSets(value, new SetEnumerationOptions { MaxSets = maxSets });
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

    public static void AssertEqual(HashSet<string> expected, HashSet<string> actual)
    {
        CollectionAssert.AreEquivalent(expected.ToList(), actual.ToList());
    }

    public static HashSet<string> Union(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.UnionWith(right);
        return output;
    }

    public static HashSet<string> Intersect(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.IntersectWith(right);
        return output;
    }

    public static HashSet<string> Difference(HashSet<string> left, HashSet<string> right)
    {
        var output = new HashSet<string>(left, StringComparer.Ordinal);
        output.ExceptWith(right);
        return output;
    }

    public static HashSet<string> Subset0(HashSet<string> family, int variable)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in family)
        {
            if (!ParseSet(set).Contains(variable))
            {
                output.Add(set);
            }
        }

        return output;
    }

    public static HashSet<string> Subset1(HashSet<string> family, int variable)
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

    public static HashSet<string> Containing(HashSet<string> family, int variable)
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

    public static HashSet<string> NotContaining(HashSet<string> family, int variable)
    {
        return Subset0(family, variable);
    }

    public static HashSet<string> Change(HashSet<string> family, int variable)
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
}
