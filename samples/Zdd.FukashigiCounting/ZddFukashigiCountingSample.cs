using System;
using System.Collections.Generic;
using DecisionDiagramSharp;

// Count all subsets of {1..N} that do not contain adjacent numbers.
// This is an original "Fukashigi-style" combinatorial counting example using ZDD.
const int n = 10;
var manager = new ZddManager();
var variables = new VariableId[n];
for (var i = 0; i < n; i++)
{
    variables[i] = manager.GetOrAddVariable((i + 1).ToString());
}

var validSets = new List<VariableId[]>();
GenerateNonAdjacentSets(variables, 0, false, new List<VariableId>(), validSets);
var family = manager.MakeFamily(validSets);

Console.WriteLine("N = " + n);
Console.WriteLine("Non-adjacent subset count: " + manager.CountSets(family));
Console.WriteLine("First 10 sets:");
var preview = manager.EnumerateSets(family, new SetEnumerationOptions { MaxSets = 500 });
var previewCount = Math.Min(10, preview.Count);
for (var i = 0; i < previewCount; i++)
{
    Console.Write("- { ");
    for (var j = 0; j < preview[i].Count; j++)
    {
        if (j > 0)
        {
            Console.Write(", ");
        }

        Console.Write(manager.GetVariableName(preview[i][j]));
    }

    Console.WriteLine(" }");
}

static void GenerateNonAdjacentSets(
    IReadOnlyList<VariableId> variables,
    int index,
    bool previousTaken,
    List<VariableId> current,
    List<VariableId[]> output)
{
    if (index == variables.Count)
    {
        output.Add(current.ToArray());
        return;
    }

    GenerateNonAdjacentSets(variables, index + 1, false, current, output);
    if (!previousTaken)
    {
        current.Add(variables[index]);
        GenerateNonAdjacentSets(variables, index + 1, true, current, output);
        current.RemoveAt(current.Count - 1);
    }
}
