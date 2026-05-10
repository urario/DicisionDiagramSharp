using System;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

// This sample demonstrates ZDD-based include-path analysis.
// String-name helpers and handle-first extension methods are used
// to show the high-level API surface.

var dd = new DecisionDiagramManager();

// Each include edge is modelled as a ZDD variable (an element of the set).
// A set in the ZDD family represents one include path.
var family = dd.Zdd.MakeFamily(new[]
{
    new[] { "A.h -> Common.h", "Common.h -> Windows.h" },
    new[] { "B.h -> LegacyBase.h", "LegacyBase.h -> Windows.h" },
    new[] { "A.h -> Common.h", "LegacyBase.h -> Windows.h" }
});

Console.WriteLine("Total paths: " + dd.Zdd.CountSets(family));

var edgeCommonWin = dd.Zdd.GetOrAddVariable("Common.h -> Windows.h");
var containingCommon = dd.Zdd.Containing(family, edgeCommonWin);
Console.WriteLine("Paths through Common.h -> Windows.h: " + dd.Zdd.CountSets(containingCommon));
Console.WriteLine();

// Export set family as Markdown using the extension method API
Console.WriteLine(family.ToMarkdownSetFamily());
Console.WriteLine();

// Export DOT graph
Console.WriteLine(family.ToDot());
