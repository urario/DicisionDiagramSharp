using System;
using DecisionDiagramSharp;

var manager = new ZddManager();
var edgeACommon = manager.GetOrAddVariable("A.h -> Common.h");
var edgeCommonWin = manager.GetOrAddVariable("Common.h -> Windows.h");
var edgeBLegacy = manager.GetOrAddVariable("B.h -> LegacyBase.h");
var edgeLegacyWin = manager.GetOrAddVariable("LegacyBase.h -> Windows.h");

var family = manager.MakeFamily(
    new[]
    {
        new[] { edgeACommon, edgeCommonWin },
        new[] { edgeBLegacy, edgeLegacyWin },
        new[] { edgeACommon, edgeLegacyWin }
    });

Console.WriteLine("Total sets: " + manager.CountSets(family));
var containingCommon = manager.Containing(family, edgeCommonWin);
Console.WriteLine("Sets containing Common.h -> Windows.h: " + manager.CountSets(containingCommon));

var sets = manager.EnumerateSets(family);
for (var i = 0; i < sets.Count; i++)
{
    Console.Write("- { ");
    for (var j = 0; j < sets[i].Count; j++)
    {
        if (j > 0)
        {
            Console.Write(", ");
        }

        Console.Write(manager.GetVariableName(sets[i][j]));
    }

    Console.WriteLine(" }");
}
