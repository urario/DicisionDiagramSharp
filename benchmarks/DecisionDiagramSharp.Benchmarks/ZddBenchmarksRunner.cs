using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;

BenchmarkSwitcher.FromAssembly(typeof(ZddBenchmarks).Assembly).Run(args);

[MemoryDiagnoser]
public class ZddBenchmarks
{
    private VariableId[] _vars = Array.Empty<VariableId>();
    private VariableId[][] _leftSets = Array.Empty<VariableId[]>();
    private VariableId[][] _rightSets = Array.Empty<VariableId[]>();

    /// <summary>
    /// Prepares reusable random input families for benchmark runs.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var manager = new ZddManager();
        _vars = new VariableId[20];
        for (var i = 0; i < _vars.Length; i++)
        {
            _vars[i] = manager.GetOrAddVariable("V" + i);
        }

        var random = new Random(20260507);
        _leftSets = CreateRandomSets(_vars, 800, random);
        _rightSets = CreateRandomSets(_vars, 800, random);
    }

    /// <summary>
    /// Measures ZDD family construction throughput.
    /// </summary>
    [Benchmark]
    public long MakeFamily()
    {
        var manager = CreateManager();
        var family = manager.MakeFamily(_leftSets);
        return manager.CountSets(family);
    }

    /// <summary>
    /// Measures ZDD union throughput.
    /// </summary>
    [Benchmark]
    public long Union()
    {
        var manager = CreateManager();
        var left = manager.MakeFamily(_leftSets);
        var right = manager.MakeFamily(_rightSets);
        var union = manager.Union(left, right);
        return manager.CountSets(union);
    }

    /// <summary>
    /// Measures ZDD difference throughput.
    /// </summary>
    [Benchmark]
    public long Difference()
    {
        var manager = CreateManager();
        var left = manager.MakeFamily(_leftSets);
        var right = manager.MakeFamily(_rightSets);
        var diff = manager.Difference(left, right);
        return manager.CountSets(diff);
    }

    private ZddManager CreateManager()
    {
        var manager = new ZddManager();
        for (var i = 0; i < _vars.Length; i++)
        {
            manager.GetOrAddVariable("V" + i);
        }

        return manager;
    }

    private static VariableId[][] CreateRandomSets(IReadOnlyList<VariableId> vars, int count, Random random)
    {
        var sets = new VariableId[count][];
        for (var i = 0; i < count; i++)
        {
            var current = new List<VariableId>();
            for (var v = 0; v < vars.Count; v++)
            {
                if (random.NextDouble() < 0.2d)
                {
                    current.Add(vars[v]);
                }
            }

            sets[i] = current.ToArray();
        }

        return sets;
    }
}

[MemoryDiagnoser]
public class BddBenchmarks
{
    private BddManager _manager = new BddManager();
    private Bdd _featureRule;

    /// <summary>
    /// Prepares reusable BDD benchmark input.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _manager = new BddManager();
        var variables = new VariableId[12];
        for (var i = 0; i < variables.Length; i++)
        {
            variables[i] = _manager.GetOrAddVariable("F" + i);
        }

        _featureRule = _manager.False;
        for (var i = 0; i < variables.Length - 1; i++)
        {
            var pair = _manager.And(_manager.Var(variables[i]), _manager.Not(_manager.Var(variables[i + 1])));
            _featureRule = _manager.Or(_featureRule, pair);
        }
    }

    /// <summary>
    /// Measures BDD ITE throughput.
    /// </summary>
    [Benchmark]
    public long Ite()
    {
        var result = _manager.Ite(_featureRule, _manager.True, _manager.False);
        return _manager.CountModels(result);
    }

    /// <summary>
    /// Measures BDD truth-table diagnostics generation.
    /// </summary>
    [Benchmark]
    public int TruthTable()
    {
        var table = BddDiagnostics.BuildTruthTable(_manager, _featureRule);
        return table.Rows.Count;
    }
}
